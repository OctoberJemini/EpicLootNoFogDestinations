#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ValheimEpicLootNoFogDestinations
{
 // No Epic Loot type references here: incompatible assemblies must be rejected before loading the engine.
 internal static class Compatibility
 {
  const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
  internal static string TypeName(Type t) {
   if(t.IsGenericParameter)return "!"+t.GenericParameterPosition;
   if(t.IsByRef)return TypeName(t.GetElementType())+"&";
   if(t.IsArray)return TypeName(t.GetElementType())+"[]";
   if(t.IsGenericType)return t.GetGenericTypeDefinition().FullName.Replace('+','/')+"<"+string.Join(",",t.GetGenericArguments().Select(TypeName).ToArray())+">";
   return t.FullName.Replace('+','/');
  }
  internal static string MethodKey(MethodBase m) => "M|"+m.DeclaringType.FullName.Replace('+','/')+"|"+m.Name+"|"+m.IsStatic+"|"+m.IsPublic+"|"+(m is MethodInfo?TypeName(((MethodInfo)m).ReturnType):"System.Void")+"|"+string.Join(",",m.GetParameters().Select(p=>TypeName(p.ParameterType)).ToArray());
  internal static string FieldKey(FieldInfo f) => "F|"+f.DeclaringType.FullName.Replace('+','/')+"|"+f.Name+"|"+f.IsStatic+"|"+f.IsPublic+"|"+TypeName(f.FieldType);
  internal static List<string> Check(Assembly assembly,IEnumerable<string> contract) {
   var failures=new List<string>();
   foreach(string entry in contract) {
    try {
     var parts=entry.Split('|');
     var type=assembly.GetType(parts[1].Replace('/','+'),false);
     if(type==null) { failures.Add("Missing type: "+parts[1]); continue; }
     bool found=false;
     if(parts[0]=="T")found=true;
     else if(parts[0]=="F")found=type.GetFields(Flags).Any(f=>FieldKey(f)==entry);
     else if(parts[0]=="M")found=type.GetMethods(Flags).Cast<MethodBase>().Concat(type.GetConstructors(Flags)).Where(m=>m.Name==parts[2]).Any(m=>MethodKey(m)==entry);
     else if(parts[0]=="E") { var f=type.GetField(parts[2],Flags); found=f!=null && Convert.ToInt64(f.GetRawConstantValue()).ToString(System.Globalization.CultureInfo.InvariantCulture)==parts[3]; }
     else if(parts[0]=="P")found=type.GetMethods(Flags).Any(m=>m.Name==parts[2] && string.Join(",",m.GetParameters().Select(p=>p.Name).ToArray())==parts[3]);
     if(!found)failures.Add("API changed: "+entry);
    } catch(Exception ex) { failures.Add("Cannot inspect "+entry+": "+ex.GetType().Name+": "+ex.Message); }
   }
   return failures;
  }
 }
}




