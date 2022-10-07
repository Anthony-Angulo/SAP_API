using AutoMapper;
using static SAP_API.Controllers.qrcontroller;
using static SAP_API.Controllers.rutasController;

namespace SAP_API.Models.Profiles
{
    public class rutaprofile : Profile
    {
        public rutaprofile()
        {
            //Source=> target
            CreateMap<AddRutaDto, rutas>();
            CreateMap<UpdateRutaDto, rutas>();
            CreateMap<CreateQR, QR_ALMACENES>();

        }
    }
}
