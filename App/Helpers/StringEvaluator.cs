//nugget: leverage the JScript DLL to provide quick string expression evals, saweet! (very handy for WPF Value Converters :) - from here: http://odetocode.com/Articles/80.aspx
using System;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.JScript;

public class StringEvaluator
{
  public static bool EvalToBool(string statement)
  {
    string s = EvalToString(statement);
    return bool.Parse(s);
  }

  
  public static int EvalToInteger(string statement)
  {
    string s = EvalToString(statement);
    return int.Parse(s.ToString());
  }

  public static double EvalToDouble(string statement)
  {
    string s = EvalToString(statement);
    return double.Parse(s);
  }

  public static string EvalToString(string statement)
  {
    object o = EvalToObject(statement);
    return o.ToString();
  }

  public static object EvalToObject(string statement)
  {
    return _evaluatorType.InvokeMember(
                "Eval",
                BindingFlags.InvokeMethod,
                null,
                _evaluator,
                new object[] { statement }
              );
  }

  static StringEvaluator()
  {
    //deprecated: ICodeCompiler compiler = new JScriptCodeProvider().CreateCompiler();
    CodeDomProvider compiler = CodeDomProvider.CreateProvider("jscript");

    CompilerParameters parameters;
    parameters = new CompilerParameters();
    parameters.GenerateInMemory = true;

    CompilerResults results;
    results = compiler.CompileAssemblyFromSource(parameters, _jscriptSource);

    Assembly assembly = results.CompiledAssembly;
    _evaluatorType = assembly.GetType("Evaluator.Evaluator");

    _evaluator = Activator.CreateInstance(_evaluatorType);
  }

  private static object _evaluator = null;
  private static Type _evaluatorType = null;
  private static readonly string _jscriptSource =

      @"package Evaluator
          {
              class Evaluator
              {
                public function Eval(expr : String) : String 
                { 
                    return eval(expr); 
                }
              }
          }";
}
