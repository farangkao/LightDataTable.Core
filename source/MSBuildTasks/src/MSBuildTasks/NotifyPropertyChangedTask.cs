using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;

namespace MSBuildTasks
{
    //This is how to incorporate this task in .csproj file:
    //
    //<Project>
    //  ...
    //  <UsingTask TaskName="MSBuildTasks.NotifyPropertyChangedTask" AssemblyFile="$(SolutionDir)..\lib\MSBuild\MSBuildTasks.dll" />
    //  <Target Name="AfterCompile">
    //    <MSBuildTasks.NotifyPropertyChangedTask AssemblyPath="$(ProjectDir)obj\$(Configuration)\$(TargetFileName)" />
    //  </Target>
    //</Project>
    public class NotifyPropertyChangedTask : Task
    {
        public override bool Execute()
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters { ReadSymbols = true });
            var module = assemblyDefinition.MainModule;  

            foreach (var type in module.Types)
            {
                foreach (var prop in type.Properties)
                {
                    foreach (var attribute in prop.CustomAttributes)
                    {
                        if (!attribute.Constructor.DeclaringType.FullName.Contains("ExcludeFromAbstract"))
                        {
                            InjectMsil(module, type, prop);
                        }
                    }
                }
            }
            assemblyDefinition.Write(this.AssemblyPath, new WriterParameters { WriteSymbols = true });
            return true;
        }

        private static void InjectMsil(ModuleDefinition module, TypeDefinition type, PropertyDefinition prop)
        {
            var msilWorker = prop.SetMethod.Body.GetILProcessor();
            var ldarg0 = msilWorker.Create(OpCodes.Ldarg_0);

            MethodDefinition raisePropertyChangedMethod = FindRaisePropertyChangedMethod(type);
            if (raisePropertyChangedMethod == null)
                throw new Exception("RaisePropertyChanged method was not found in type " + type.FullName);
            
            var raisePropertyChanged = module.Import(raisePropertyChangedMethod);
            var propertyName = msilWorker.Create(OpCodes.Ldstr, prop.Name);
            var callRaisePropertyChanged = msilWorker.Create(OpCodes.Callvirt, raisePropertyChanged);
            msilWorker.InsertBefore(prop.SetMethod.Body.Instructions[prop.SetMethod.Body.Instructions.Count - 1], ldarg0);
            msilWorker.InsertAfter(ldarg0, propertyName);
            msilWorker.InsertAfter(propertyName, callRaisePropertyChanged);
        }

        private static MethodDefinition FindRaisePropertyChangedMethod(TypeDefinition type)
        {
            foreach (var method in type.Methods)
            {
                if (method.Name == "RaisePropertyChanged"
                    && method.Parameters.Count == 1
                    && method.Parameters[0].ParameterType.FullName == "System.String")
                {
                    return method;
                }
            }
            if (type.BaseType.FullName == "System.Object")
                return null;
            return FindRaisePropertyChangedMethod(type.BaseType.Resolve());
        }

        [Required]
        public string AssemblyPath { get; set; }
    }
}