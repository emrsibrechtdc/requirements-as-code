using AutoMapper;
using Platform.Customers.Domain.Customers;
using Platform.Customers.Application.Customers.Dtos;

namespace Platform.Customers.Application.Profiles;

public class AutoMapProfile : Profile
{
    public AutoMapProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.ContactFirstName, opt => opt.MapFrom(src => src.ContactInfo.FirstName))
            .ForMember(dest => dest.ContactLastName, opt => opt.MapFrom(src => src.ContactInfo.LastName))
            .ForMember(dest => dest.ContactFullName, opt => opt.MapFrom(src => src.ContactInfo.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ContactInfo.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.ContactInfo.PhoneNumber))
            .ForMember(dest => dest.AddressLine1, opt => opt.MapFrom(src => src.Address.AddressLine1))
            .ForMember(dest => dest.AddressLine2, opt => opt.MapFrom(src => src.Address.AddressLine2))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address.State))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Address.PostalCode))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address.Country))
            .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.Address.FullAddress));

        CreateMap<Customer, CustomerResponse>()
            .ForCtorParam("CustomerCode", opt => opt.MapFrom(src => src.CustomerCode))
            .ForCtorParam("CompanyName", opt => opt.MapFrom(src => src.CompanyName))
            .ForCtorParam("ContactFullName", opt => opt.MapFrom(src => src.ContactInfo.FullName))
            .ForCtorParam("Email", opt => opt.MapFrom(src => src.ContactInfo.Email))
            .ForCtorParam("IsActive", opt => opt.MapFrom(src => src.IsActive))
            .ForCtorParam("CreatedAt", opt => opt.MapFrom(src => src.CreatedAt.DateTime))
            .ForCtorParam("CreatedBy", opt => opt.MapFrom(src => src.CreatedBy ?? string.Empty));
    }
}