﻿using AutoMapper;
using Fan.Data;
using Fan.Enums;
using Fan.Helpers;
using Fan.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Fan.Tests.Data
{
    /// <summary>
    /// Base class for all integration tests.  It helps initialization of a in-memory based 
    /// FanDbContext as well as seeding initial blog data that some of the tests depend on.
    /// </summary>
    /// <remarks>
    /// When it comes to test with an in-memory database, there are two choices, the 
    /// EF Core In-Memory Database Provider (Microsoft.EntityFrameworkCore.InMemory)
    /// or the SQLite Database Provider (Microsoft.EntityFrameworkCore.Sqlite) with the SQLite 
    /// in-memory mode. However EF Core provider does not enforce any integrity like a relational 
    /// database, for example, the Meta table cannot have duplicate keys, it doesn't enforce that.
    /// 
    /// For more info https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/index
    /// </remarks>
    public class DataTestBase : IDisposable
    {
        /// <summary>
        /// A <see cref="FanDbContext"/> built with Sqlite in-memory mode.
        /// </summary>
        protected FanDbContext _db;
        /// <summary>
        /// A logger factory for sub class to create their type dependent logger.
        /// </summary>
        protected ILoggerFactory _loggerFactory;
        /// <summary>
        /// A <see cref="IDistributedCache"/> for tests for BLL, DAL tests don't use this.
        /// </summary>
        protected IDistributedCache _cache;
        /// <summary>
        /// A <see cref="IMapper"/> for tests for BLL, DAL tests don't use this.
        /// </summary>
        protected IMapper _mapper;

        public DataTestBase()
        {
            _db = GetContextWithSqlite(); // I can either do sqlite in-mem mode or ef core in-mem db
            var serviceProvider = new ServiceCollection().AddMemoryCache().AddLogging().BuildServiceProvider();
            var memCacheOptions = serviceProvider.GetService<IOptions<MemoryDistributedCacheOptions>>();
            _cache = new MemoryDistributedCache(memCacheOptions);
            _loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _mapper = Util.Mapper;
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted(); // important, otherwise SeedTestData is not erased
            _db.Dispose();
        }

        // -------------------------------------------------------------------- Seed data

        public const string POST_SLUG = "test-post";
        public const string CAT_TITLE = "Technology";
        public const string CAT_SLUG = "tech";
        public const string TAG1_TITLE = "asp.net";
        public const string TAG2_TITLE = "c#";
        public const string TAG1_SLUG = "aspnet";
        public const string TAG2_SLUG = "cs";

        /// <summary>
        /// Seeds 1 user.
        /// </summary>
        protected void SeedUser()
        {
            _db.Users.Add(Actor.User);
            _db.SaveChanges();
        }

        /// <summary>
        /// Seeds 1 blog post associated with 1 category and 2 tags.
        /// </summary>
        /// <param name="db"></param>
        protected void SeedTestPost()
        {
            _db.Users.Add(Actor.User);
            _db.Metas.Add(new Meta { Key = "SiteSettings", Value = JsonConvert.SerializeObject(new SiteSettings()) });
            _db.Metas.Add(new Meta { Key = "BlogSettings", Value = JsonConvert.SerializeObject(new BlogSettings()) });
            _db.Posts.Add(GetPost());
            _db.SaveChanges();
        }

        /// <summary>
        /// Seeds a specified number of posts, even number posts are drafts and tagged with tag2, 
        /// while odd number posts are published and tagged with tag1.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="numOfPosts"></param>
        protected void SeedTestPosts(int numOfPosts)
        {
            _db.Users.Add(Actor.User);
            _db.Metas.Add(new Meta { Key = "SiteSettings", Value = JsonConvert.SerializeObject(new SiteSettings()) });
            _db.Metas.Add(new Meta { Key = "BlogSettings", Value = JsonConvert.SerializeObject(new BlogSettings()) });
            _db.Posts.AddRange(GetPosts(numOfPosts));
            _db.SaveChanges();
        }

        /// <summary>
        /// Returns a post associated with 1 category and 2 tags.
        /// </summary>
        private Post GetPost()
        {
            var cat = new Category { Slug = CAT_SLUG, Title = CAT_TITLE };
            var tag1 = new Tag { Slug = TAG1_SLUG, Title = TAG1_TITLE };
            var tag2 = new Tag { Slug = TAG2_SLUG, Title = TAG2_TITLE };

            var post = new Post
            {
                Body = "A post body.",
                Category = cat,
                UserId = Actor.AUTHOR_ID,
                CreatedOn = (new DateTime(2017, 01, 01)).ToUniversalTime(),
                RootId = null,
                Title = "A published post",
                Slug = POST_SLUG,
                Type = EPostType.BlogPost,
                Status = EPostStatus.Published,
            };
            // this is outside because we are using post itself to create PostTag
            post.PostTags = new List<PostTag> {
                    new PostTag { Post = post, Tag = tag1 },
                    new PostTag { Post = post, Tag = tag2 },
                };

            return post;
        }

        /// <summary>
        /// Returns a specified number of posts, even number posts are drafts and tagged with tag2, 
        /// while odd number posts are published and tagged with tag1.
        /// </summary>
        /// <returns></returns>
        private List<Post> GetPosts(int numOfPosts)
        {
            if (numOfPosts < 1) throw new ArgumentException("Param numOfPosts must be > 1");

            var cat = new Category { Slug = CAT_SLUG, Title = CAT_TITLE };
            var tag1 = new Tag { Slug = TAG1_SLUG, Title = TAG1_TITLE };
            var tag2 = new Tag { Slug = TAG2_SLUG, Title = TAG2_TITLE };

            var list = new List<Post>();
            for (int i = 1; i <= numOfPosts; i++)
            {
                var post = new Post
                {
                    Body = $"A post body #{i}.",
                    Category = cat,
                    UserId = Actor.AUTHOR_ID,
                    CreatedOn = new DateTime(2017, 01, i), // be aware this is UTC time
                    RootId = null,
                    Title = $"Test Post #{i}",
                    Slug = $"{POST_SLUG}-{i}",
                    Type = EPostType.BlogPost,
                    Status = (i % 2 == 0) ? EPostStatus.Draft : EPostStatus.Published, // drafts / published
                };

                if (i % 2 == 0)
                {
                    post.PostTags = new List<PostTag> { // posts tagged c#
                        new PostTag { Post = post, Tag = tag2 },
                    };
                }
                else
                {
                    post.PostTags = new List<PostTag> { // posts tagged asp.net
                        new PostTag { Post = post, Tag = tag1 },
                    };
                }

                list.Add(post);
            }

            return list;
        }

        // -------------------------------------------------------------------- DbContext

        /// <summary>
        /// Returns <see cref="FanDbContext"/> with SQLite Database Provider in-memory mode.
        /// </summary>
        private FanDbContext GetContextWithSqlite()
        {
            var connection = new SqliteConnection() { ConnectionString = "Data Source=:memory:" };
            connection.Open();

            var builder = new DbContextOptionsBuilder<FanDbContext>();
            builder.UseSqlite(connection);

            var context = new FanDbContext(builder.Options);
            context.Database.EnsureCreated();

            return context;
        }

        /// <summary>
        /// Returns <see cref="FanDbContext"/> with Entity Framework Core In-Memory Database.
        /// </summary>
        private FanDbContext GetContextWithEFCore()
        {
            var _options = new DbContextOptionsBuilder<FanDbContext>().UseInMemoryDatabase("FanInMemDb").Options;
            return new FanDbContext(_options);
        }
    }
}
