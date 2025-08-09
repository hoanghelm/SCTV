using Common.ApiResponse;
using Common.Constants;
using Common.Enums;
using Common.ErrorResult;
using Common.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Paginate;
using Streaming.Data.Context;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Streaming.ViewModels;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Streaming.Queries
{
    public class GetPostsHandler : IRequestHandler<GetPostsRequest, ApiResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;

        public GetPostsHandler(IUnitOfWork<ReadDbContext> unitOfWork, IDistributedCache cache, ILogger<GetPostsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResult> Handle(GetPostsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = CacheKey.LIST_POST + JsonSerializer.Serialize(request);
                var post = await _cache.GetCacheValueAsync<Paginate<PostViewModel>>(cacheKey);

                if (post == null)
                {
                    post = await GetPostsInDatabase(request, cancellationToken);
                    _ = _cache.SetCacheValueAsync(cacheKey, post, 60 * 1);
                }

                return ApiResult.Succeeded(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return ApiResult.Failed(HttpCode.InternalServerError);
            }
        }

        private async Task<Paginate<PostViewModel>> GetPostsInDatabase(GetPostsRequest request, CancellationToken cancellationToken)
        {
            var predicate = BuildFilterQuery(request);
            var orderBy = BuildOrderQuery(request);
            var products = await _unitOfWork.GetRepository<PostEntity>().GetPagingListAsync(
                selector: n => new PostViewModel(n),
                predicate: predicate,
                orderBy: orderBy,
                page: request.Page,
                size: request.Size,
                cancellationToken: cancellationToken);

            return (Paginate<PostViewModel>)products;
        }

        private static Func<IQueryable<PostEntity>, IOrderedQueryable<PostEntity>> BuildOrderQuery(GetPostsRequest request)
        {
            var monthBefore = DateTime.UtcNow.AddMonths(-1);
            Expression<Func<PostEntity, object>> expression = request.SortBy switch
            {
                SortBy.LATEST => p => p.CreatedAt,
                SortBy.TRENDING => p => p.UpdatedAt,
                _ => throw new NotImplementedException()
            };

            return request.SortType == SortType.ASC
                ? o => o.OrderBy(expression)
                : o => o.OrderByDescending(expression);
        }

        private static Expression<Func<PostEntity, bool>> BuildFilterQuery(GetPostsRequest request)
        {
            Expression<Func<PostEntity, bool>> filterQuery = p => true;

            if (request.Search.IsNotEmpty())
            {
                request.Search = request.Search.Trim().ToLower();
                filterQuery = filterQuery.AndAlso(p => p.Title.ToLower().Contains(request.Search));
            }

            if (request.Slug.IsNotEmpty())
            {
                filterQuery = filterQuery.AndAlso(p => p.PostCategories.Where(c => c.Post.Slug == request.Slug).Any());
            }

            return filterQuery;
        }
    }
}
