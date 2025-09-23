using AutoMapper;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Domain.Locations;

namespace Platform.Locations.Application.Profiles;

public class AutoMapProfile : Profile
{
    public AutoMapProfile()
    {
        CreateMap<Location, LocationDto>()
            .ForMember(dest => dest.LocationCode, opt => opt.MapFrom(src => src.LocationCode))
            .ForMember(dest => dest.LocationTypeCode, opt => opt.MapFrom(src => src.LocationTypeCode))
            .ForMember(dest => dest.LocationTypeName, opt => opt.MapFrom(src => src.LocationTypeName))
            .ForMember(dest => dest.AddressLine1, opt => opt.MapFrom(src => src.AddressLine1))
            .ForMember(dest => dest.AddressLine2, opt => opt.MapFrom(src => src.AddressLine2))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.ZipCode))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country));

        CreateMap<Location, LocationResponse>()
            .ForMember(dest => dest.LocationCode, opt => opt.MapFrom(src => src.LocationCode));
    }
}