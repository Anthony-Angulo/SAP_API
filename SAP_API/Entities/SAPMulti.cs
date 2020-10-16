
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SAP_API.Entities {
    public class SAPMulti {

        private SAPContext[] SAPContextList;
        private byte CurrentInstance = 0;
        private const byte InstancesCount = 5;

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

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < SAPContextList.Length; i++) {
                
                if (SAPContextList[i].oCompany.Connected) {
                    continue;
                }

                var index = i;
                var task = Task.Run(() => {
                    Console.WriteLine($"No. Instancia {index} Conectando...");
                    int ResultCode = SAPContextList[index].oCompany.Connect();

                    if (ResultCode != 0) {
                        string error = SAPContextList[index].oCompany.GetLastErrorDescription();
                        Console.WriteLine($"Error en Instancia {index} Al Intentar Conectar:");
                        Console.WriteLine($"Code: {ResultCode}. Error: {error}");
                        Errors.AppendLine($"Code: {ResultCode}. Error: {error}");
                    } else {
                        Console.WriteLine($"Instancia {index} Conectada Correctamente");
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

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
