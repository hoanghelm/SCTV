using AutoMapper;
using Streaming.Domain.Entities;
using Streaming.Service.ViewModels;

namespace Streaming.Service.Mappings
{
	public class StreamingProfile : Profile
	{
		public StreamingProfile()
		{
			CreateMap<Camera, CameraViewModel>();
			CreateMap<CameraViewModel, Camera>();

			CreateMap<StreamSession, StreamSessionViewModel>();
			CreateMap<StreamSessionViewModel, StreamSession>();
		}
	}
}