﻿using Fan.Enums;
using Fan.Helpers;
using Fan.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fan.Validators
{
    /// <summary>
    /// Validator for <see cref="Category"/> and <see cref="Tag"/>.
    /// </summary>
    /// <remarks>
    /// Both Category and Tag need validation on its title, the slug is derived from title and is made sure
    /// to be valid.
    /// </remarks>
    public class TaxonomyValidator : AbstractValidator<Taxonomy>
    {
        public TaxonomyValidator(IEnumerable<string> existingTitles, ETaxonomyType type)
        {
            RuleFor(c => c.Title)
                .NotEmpty()
                .Length(1, Const.TAXONOMY_TITLE_SLUG_MAXLEN)
                .Must(title => !existingTitles.Contains(title, StringComparer.CurrentCultureIgnoreCase)) 
                .WithMessage(c => $"{type} '{c.Title}' is not available, please choose a different one.");
        }
    }
}