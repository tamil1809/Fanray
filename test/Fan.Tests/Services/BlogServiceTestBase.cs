﻿using AutoMapper;
using Fan.Data;
using Fan.Helpers;
using Fan.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Fan.Tests.Services
{
    /// <summary>
    /// Base class for <see cref="BlogService"/> unit tests.
    /// </summary>
    public class BlogServiceTestBase
    {
        protected Mock<IPostRepository> _postRepoMock;
        protected Mock<IMetaRepository> _metaRepoMock;
        protected Mock<ICategoryRepository> _catRepoMock;
        protected Mock<ITagRepository> _tagRepoMock;
        protected BlogService _blogSvc;
        protected SettingService _settingSvc;
        protected IMapper _mapper;
        protected IDistributedCache _cache;
        protected ILogger<BlogService> _loggerBlogSvc;
        protected ILogger<SettingService> _loggerSettingSvc;

        /// <summary>
        /// Base constructor which will be called first for each test in derived test classes, thus
        /// setting up mocked repos and components here.
        /// </summary>
        public BlogServiceTestBase()
        {
            // repos
            _postRepoMock = new Mock<IPostRepository>();
            _metaRepoMock = new Mock<IMetaRepository>();
            _catRepoMock = new Mock<ICategoryRepository>();
            _tagRepoMock = new Mock<ITagRepository>();

            // cache
            var serviceProvider = new ServiceCollection().AddMemoryCache().AddLogging().BuildServiceProvider();
            var memCacheOptions = serviceProvider.GetService<IOptions<MemoryDistributedCacheOptions>>();
            _cache = new MemoryDistributedCache(memCacheOptions);

            // logger
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _loggerBlogSvc = loggerFactory.CreateLogger<BlogService>();
            _loggerSettingSvc = loggerFactory.CreateLogger<SettingService>();

            // mapper
            _mapper = Util.Mapper;

            // svc
            _settingSvc = new SettingService(_metaRepoMock.Object, _cache, _loggerSettingSvc);
            _blogSvc = new BlogService(_settingSvc, _catRepoMock.Object, _metaRepoMock.Object, _postRepoMock.Object, _tagRepoMock.Object,
                _cache, _loggerBlogSvc, _mapper);
        }
    }
}
