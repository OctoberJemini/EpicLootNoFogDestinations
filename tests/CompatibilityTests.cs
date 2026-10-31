using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ValheimEpicLootNoFogDestinations;
class CompatibilityTests {
 public class Fixture { public static string Value=""; public int Method(string input){return input.Length;} }
 static void Main(string[] args) {
  // Exercise the production checker against compatible and incompatible signatures.
  var self=typeof(Fixture).Assembly;
  var valid=new[]{Compatibility.FieldKey(typeof(Fixture).GetField("Value")),Compatibility.MethodKey(typeof(Fixture).GetMethod("Method"))};
  if(Compatibility.Check(self,valid).Count!=0)throw new Exception("Compatible fixture rejected");
  foreach(var broken in new[]{valid[0].Replace("|True|","|False|"),valid[0].Replace("System.String","System.Int32"),valid[1].Replace("|Method|","|Missing|"),valid[1].Replace("System.String","System.Int32"),"T|Missing.Type","P|CompatibilityTests/Fixture|Method|renamed"})
   if(Compatibility.Check(self,new[]{broken}).Count!=1)throw new Exception("Breaking change accepted: "+broken);
  var files=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  foreach(string root in args.Skip(1))foreach(var file in Directory.GetFiles(root,"*.dll",SearchOption.AllDirectories)) {
   string name=Path.GetFileNameWithoutExtension(file); if(!files.ContainsKey(name))files.Add(name,file);
  }
  AppDomain.CurrentDomain.AssemblyResolve+=(sender,e)=> {
   var name=new AssemblyName(e.Name);
   if(name.Name=="EpicLoot")return Assembly.LoadFrom(args[0]);
   if(files.TryGetValue(name.Name,out var path))return Assembly.LoadFrom(path);
   return Assembly.Load(e.Name);
  };
  var assembly=Assembly.LoadFrom(args[0]);
  var failures=Compatibility.Check(assembly,ApiContract.Entries);
  Console.WriteLine(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(args[0])))+": "+(failures.Count==0?"PASS":"FAIL")+" ("+ApiContract.Entries.Length+" contracts; 7 checker fixture checks)");
  foreach(var failure in failures)Console.WriteLine(failure);
  Environment.ExitCode=failures.Count==0?0:1;
 }
}


