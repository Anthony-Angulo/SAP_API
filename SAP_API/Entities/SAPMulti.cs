
using SAP_API.Models;
using System;
using System.Text;

namespace SAP_API.Entities {
    public class SAPMulti {

        private SAPContext[] SAPContextList;
        private byte CurrentInstance = 0;
        private const byte InstancesCount = 1;

        public SAPMulti() {
            Console.WriteLine($"Arreglo de Instancias Inicializado");
            SAPContextList = new SAPContext[InstancesCount];
        }

        public void Init() {
            for (int i = 0; i < SAPContextList.Length; i++) {
                Console.WriteLine($"No. Instancia Inicializada: {i}");
                SAPContextList[i] = new SAPContext();
            }
        }

        public SAPContext GetCurrentInstance() {
            if (CurrentInstance >= InstancesCount) {
                CurrentInstance = 0;
            }

            if (SAPContextList[CurrentInstance].oCompany.InTransaction) {
                return null;
            }

            Console.WriteLine($"No. de Instancia Inicio Uso: {CurrentInstance}");
            return SAPContextList[CurrentInstance++];
        }

        //  Note: This could be Asynchronous
        public SAPConnectResult ConnectAll() {

            StringBuilder Errors = new StringBuilder();

            for (int i = 0; i < SAPContextList.Length; i++) {
                
                if (SAPContextList[i].oCompany.Connected) {
                    continue;
                }

                Console.WriteLine($"No. Instancia {i} Conectando...");
                int ResultCode = SAPContextList[i].oCompany.Connect();
                
                if (ResultCode != 0) {
                    string error = SAPContextList[i].oCompany.GetLastErrorDescription();
                    Console.WriteLine($"Error en Instancia {i} Al Intentar Conectar:");
                    Console.WriteLine($"Code: {ResultCode}. Error: {error}");
                    Errors.AppendLine($"Code: {ResultCode}. Error: {error}");
                } else {
                    Console.WriteLine($"Instancia {i} Conectada Correctamente");
                }
            }

            SAPConnectResult Result = new SAPConnectResult {
                Result = (Errors.Length == 0),
                Errors = Errors.ToString()
            };

            return Result;

        }

        public class SAPConnectResult {
            public string Errors;
            public bool Result;
        }

    }
}
