//nugget: leverage the JScript DLL to provide quick string expression evals, saweet! (very handy for WPF Value Converters :) - from here: http://odetocode.com/Articles/80.aspx
using System;
using System.CodeDom.Compiler;
using System.Reflection;

// ReSharper disable CheckNamespace
public class StringEvaluator
// ReSharper restore CheckNamespace
{
  public static bool EvalToBool(string statement)
  {
    string s = EvalToString(statement);
    return bool.Parse(s);
  }

  
  public static int EvalToInteger(string statement)
  {
    return int.Parse(EvalToString(statement));
  }

  public static double EvalToDouble(string statement)
  {
    return double.Parse(EvalToString(statement));
  }

  public static string EvalToString(string statement)
  {
    return EvalToObject(statement).ToString();
  }

  public static object EvalToObject(string statement)
  {
    return EvaluatorType.InvokeMember(
                "Eval",
                BindingFlags.InvokeMethod,
                null,
                Evaluator,
                new object[] { statement }
              );
  }

  static StringEvaluator()
  {
    //deprecated: ICodeCompiler compiler = new JScriptCodeProvider().CreateCompiler();
    var compiler = CodeDomProvider.CreateProvider("jscript");

    var parameters = new CompilerParameters {GenerateInMemory = true};

    var results = compiler.CompileAssemblyFromSource(parameters, JscriptSource);

    var assembly = results.CompiledAssembly;
    EvaluatorType = assembly.GetType("Evaluator.Evaluator");

    Evaluator = Activator.CreateInstance(EvaluatorType);
  }

  private static readonly object Evaluator;
  private static readonly Type EvaluatorType;
  private const string JscriptSource = @"package Evaluator
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
