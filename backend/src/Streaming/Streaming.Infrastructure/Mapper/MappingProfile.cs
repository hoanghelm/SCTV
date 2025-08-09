using AutoMapper;
using Streaming.Domain.Entities;
using Streaming.Service.Streaming.Commands;
using Common.ApiResponse;
using System;

namespace Streaming.Infrastructure.Mapper
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			// Post
			CreateMap<CreatePostRequest, PostEntity>(MemberList.None)
				.ForMember(dest => dest.PostCategories, opt => opt.Ignore())
				.ForMember(dest => dest.PostComments, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
		}
	}
}