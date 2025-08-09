using Streaming.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streaming.Service.Streaming.ViewModels
{
    public class PostViewModel
    {
        public PostViewModel()
        {
        }

        public PostViewModel(PostEntity entity)
        {
            Content = entity.Content;
            Excerpt = entity.Excerpt;
            Id = entity.Id;
            LastModified = entity.LastModified;
            PubDate = entity.PubDate;
            Slug = entity.Slug;
            Title = entity.Title;
            CreatedBy = entity.CreatedBy;
        }

        public string Content { get; set; }
        public string Excerpt { get; set; }
        public Guid Id { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime PubDate { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string CreatedBy { get; set; }
    }
}
