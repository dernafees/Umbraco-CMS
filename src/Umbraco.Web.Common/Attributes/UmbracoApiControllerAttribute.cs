﻿using System;
using Umbraco.Web.Common.ApplicationModels;

namespace Umbraco.Web.Common.Attributes
{
    /// <summary>
    /// When present on a controller then <see cref="UmbracoApiBehaviorApplicationModelProvider"/> conventions will apply
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class UmbracoApiControllerAttribute : Attribute
    {
    }
}
