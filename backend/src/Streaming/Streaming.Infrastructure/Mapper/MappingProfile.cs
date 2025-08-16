using AutoMapper;
using Common.ApiResponse;
using Streaming.Domain.Entities;
using Streaming.Service.ViewModels;
using System;

namespace Streaming.Infrastructure.Mapper
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Camera, CameraViewModel>()
				// Handle nullable to non-nullable string mappings
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
				.ForMember(dest => dest.StreamUrl, opt => opt.MapFrom(src => src.StreamUrl ?? string.Empty))
				.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? string.Empty))
				.ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location ?? string.Empty))
				.ForMember(dest => dest.CameraType, opt => opt.MapFrom(src => src.CameraType ?? string.Empty))
				.ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand ?? string.Empty))
				.ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model ?? string.Empty))
				.ForMember(dest => dest.Resolution, opt => opt.MapFrom(src => src.Resolution ?? string.Empty));

			CreateMap<CameraViewModel, Camera>()
				// Ignore properties that don't exist in ViewModel
				.ForMember(dest => dest.ConfigurationJson, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
				// Ignore navigation properties
				.ForMember(dest => dest.StreamSessions, opt => opt.Ignore())
				.ForMember(dest => dest.CameraPermissions, opt => opt.Ignore())
				.ForMember(dest => dest.AlertRules, opt => opt.Ignore());

			CreateMap<StreamSession, StreamSessionViewModel>();
			CreateMap<StreamSessionViewModel, StreamSession>();
		}
	}
}
