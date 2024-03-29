﻿
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SAP_API.Entities
{
    public class SAPMulti
    {

        private SAPContext
            [] SAPContextList;
        private byte CurrentInstance = 0;
        private const byte InstancesCount = 15;
        private bool Connecting = false;
        private const byte CyclesCount = 1;

        public SAPMulti()
        {
            Console.WriteLine($"Arreglo de Instancias Inicializado");
            SAPContextList = new SAPContext[InstancesCount];
        }

        public void Init()
        {
            for (int i = 0; i < SAPContextList.Length; i++)
            {
                Console.WriteLine($"No. Instancia Inicializada: {i}");
                SAPContextList[i] = new SAPContext();
            }
        }

        public SAPContext IncrementInstance()
        {

            Stopwatch sw = new Stopwatch();

            sw.Start();

            int i = 0;
            int j = 0;

            while (i < CyclesCount)
            {
                j = 0;

                while (j < SAPContextList.Length)
                {

                    CurrentInstance++;
                    if (CurrentInstance >= InstancesCount)
                    {
                        CurrentInstance = 0;
                    }

                    if (!SAPContextList[CurrentInstance].oCompany.InTransaction)
                    {
                        sw.Stop();
                        Console.WriteLine($"Instancia Numero: {CurrentInstance}");
                        Console.WriteLine($"Tiempo de Recorrido Encontrando una instancia: {sw.Elapsed}");
                        return SAPContextList[CurrentInstance];
                    }

                    j++;
                }


                i++;
            }

            sw.Stop();
            Console.WriteLine($"Tiempo de Recorrido sin encontrar instancia: {sw.Elapsed}");

            return null;
        }

        public SAPContext GetCurrentInstance()
        {
            if (CurrentInstance >= InstancesCount)
            {
                CurrentInstance = 0;
            }

            if (SAPContextList[CurrentInstance].oCompany.InTransaction)
            {
                Console.WriteLine($"No. de Instancia de falla: {CurrentInstance}");
                return null;
            }

            Console.WriteLine($"No. de Instancia Inicio Uso: {CurrentInstance}");
            return SAPContextList[CurrentInstance];
        }

        public SAPConnectResult ConnectAll()
        {

            Connecting = true;

            StringBuilder Errors = new StringBuilder();

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < SAPContextList.Length; i++)
            {

                if (SAPContextList[i].oCompany.Connected)
                {
                    continue;
                }


                var index = i;
                var task = Task.Run(() =>
                {
                    Console.WriteLine($"No. Instancia {index} Conectando...");
                    int ResultCode = SAPContextList[index].oCompany.Connect();

                    if (ResultCode != 0)
                    {
                        string error = SAPContextList[index].oCompany.GetLastErrorDescription();
                        Console.WriteLine($"Error en Instancia {index} Al Intentar Conectar:");
                        Console.WriteLine($"Code: {ResultCode}. Error: {error}");
                        Errors.AppendLine($"Code: {ResultCode}. Error: {error}");
                    }
                    else
                    {
                        Console.WriteLine($"Instancia {index} Conectada Correctamente");
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            SAPConnectResult Result = new SAPConnectResult
            {
                Result = (Errors.Length == 0),
                Errors = Errors.ToString()
            };

            Connecting = false;

            return Result;

        }

        public bool IsConnecting()
        {
            return Connecting;
        }

        public bool AllInstanceHaveConnection()
        {
            for (int i = 0; i < SAPContextList.Length; i++)
            {
                if (!SAPContextList[i].oCompany.Connected)
                {
                    return false;
                }
            }
            return true;
        }

        public class SAPConnectResult
        {
            public string Errors;
            public bool Result;
        }

    }
}
