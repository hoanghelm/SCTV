using Common.ApiResponse;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Service.Streaming.Commands
{
    public class CreateCommentRequest : IRequest<ApiResult>
    {
        [Required]
        public string Author { get; set; }
        [Required]
        public string Content { get; set; }
        public string Email { get; set; }
        public string AuthorName { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime PubDate { get; set; }
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
    }
}
