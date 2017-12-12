﻿using System;
using System.Collections.Generic;

namespace Nop.Core.Domain.Blogs
{
    /// <summary>
    /// Blog extensions
    /// </summary>
    public static class BlogExtensions
    {
        /// <summary>
        /// Parse tags
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        /// <returns>Tags</returns>
        public static string[] ParseTags(this BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            var parsedTags = new List<string>();
            if (!string.IsNullOrEmpty(blogPost.Tags))
            {
                var tags2 = blogPost.Tags.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag2 in tags2)
                {
                    var tmp = tag2.Trim();
                    if (!string.IsNullOrEmpty(tmp))
                        parsedTags.Add(tmp);
                }
            }
            return parsedTags.ToArray();
        }
    }
}
