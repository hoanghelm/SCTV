using AutoMapper;
using Common.ApiResponse;
using Common.Constants;
using Common.Enums;
using Common.ErrorResult;
using Common.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Post.Data.Context;
using Post.Domain.Contracts;
using Post.Domain.Entities;
using Post.Service.Post.ViewModels;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Post.Service.Post.Commands
{
    public class CreateCommentHandler : IRequestHandler<CreateCommentRequest, ApiResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;

        public CreateCommentHandler(IUnitOfWork<WriteDbContext> unitOfWork, IDistributedCache cache, ILogger<CreateCommentHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResult> Handle(CreateCommentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var comment = new CommentEntity()
                {
                    Id = Guid.NewGuid(),
                    Author = request.Author,
                    Content = request.Content,
                    Email = request.Email,
                    PostId = request.PostId,
                    PubDate = request.PubDate,
                    IsAdmin = request.IsAdmin
                };

                await _unitOfWork.GetRepository<CommentEntity>().InsertAsync(comment, cancellationToken);
                await _unitOfWork.CommitAsync();

                var cacheKey = CacheKey.COMMENT_POST + JsonSerializer.Serialize(comment.Id);

                var cmt = new CommentStatusNotification()
                {
                    PostId = comment.Id,
                    AuthorId = comment.Author,
                    AuthorName = request.AuthorName,
                    Email = comment.Email,
                    NotificationStatus = ComunicationStatus.READY,
                };

                _ = _cache.SetCacheValueAsync(cacheKey, cmt, 3);

                return ApiResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return ApiResult.Failed(HttpCode.InternalServerError);
            }
        }
    }
}
