using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Solink.AddIn.Helpers.Test
{
    /// <summary>
    /// A class to test <see cref="RestartableAddInGenerator"/>.
    /// </summary>
    [TestClass]
    public class RestartableAddInGeneratorTest
    {
        const string NamespaceName = "Solink.Sample";
        const string HostViewFullName = "Solink.HostViews.IThing";
        const string ClassName = "RestartableThing";
        const string ExpectedRestartableThing = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.AddIn.Helpers.Test.IThing>, Solink.AddIn.Helpers.Test.IThing
    {
        
        public RestartableThing(Solink.AddIn.Helpers.AddInFacade facade, System.AddIn.Hosting.AddInToken token, System.AddIn.Hosting.Platform platform) : 
                base(facade, token, platform)
        {
        }
        
        public int Id
        {
            get
            {
                return base.Func(_ => _.Id);
            }
            set
            {
                base.Action(_ => _.Id = value);
            }
        }
        
        public static Solink.AddIn.Helpers.Test.IThing Factory(Solink.AddIn.Helpers.AddInFacade facade, System.AddIn.Hosting.AddInToken token, System.AddIn.Hosting.Platform platform)
        {
            return new RestartableThing(facade, token, platform);
        }
        
        public int ComputeAnswerToLifeAndUniverseEverything()
        {
            return base.Func(_ => _.ComputeAnswerToLifeAndUniverseEverything());
        }
        
        public void AddToList(System.Collections.Generic.IList<string> strings)
        {
            base.Action(_ => _.AddToList(strings));
        }
    }
}
";

        private static void AssertGeneratedCode(string expected, CodeCompileUnit compileUnit)
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options = new CodeGeneratorOptions
            {
                BracingStyle = "C",
            };
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);
            }
            var actual = sb.ToString();
            AssertGeneratedLinesEqual(expected, actual);
        }

        [TestMethod]
        public void AssertGeneratedLinesEqualWhenInputsOnlyContainHeader()
        {
            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------";
            const string actual = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------";

            AssertGeneratedLinesEqual(expected, actual);
        }

        [TestMethod, ExpectedException(typeof(AssertFailedException))]
        public void AssertGeneratedLinesEqualWhenFirstLineDifferent()
        {
            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
";
            const string actual = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
foo";

            AssertGeneratedLinesEqual(expected, actual);
        }

        [TestMethod, ExpectedException(typeof(AssertFailedException))]
        public void AssertGeneratedLinesEqualWhenSecondLineDifferent()
        {
            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

";
            const string actual = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

foo";

            AssertGeneratedLinesEqual(expected, actual);
        }

        [TestMethod, ExpectedException(typeof(AssertFailedException))]
        public void AssertGeneratedLinesEqualWhenThirdLineDifferent()
        {
            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{";
            const string actual = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
[";

            AssertGeneratedLinesEqual(expected, actual);
        }

        [TestMethod, ExpectedException(typeof(AssertFailedException))]
        public void AssertGeneratedLinesEqualWhenActualIsLonger()
        {
            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample";
            const string actual = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
[";

            AssertGeneratedLinesEqual(expected, actual);
        }

        [TestMethod, ExpectedException(typeof(AssertFailedException))]
        public void AssertGeneratedLinesEqualWhenExpectedIsLonger()
        {
            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{";
            const string actual = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample";

            AssertGeneratedLinesEqual(expected, actual);
        }

        private static void AssertGeneratedLinesEqual(string expected, string actual)
        {
            var lineNumber = 0;
            using (var er = new StringReader(expected))
            using (var ar = new StringReader(actual))
            {
                var checkLine = new Func<bool>(() =>
                {
                    lineNumber++;
                    var innerEl = er.ReadLine();
                    var innerAl = ar.ReadLine();
                    // must format separately from Assert.AreEqual in case expected or actual contain curly braces
                    var message = String.Format("First difference is at line {0}", lineNumber);
                    Assert.AreEqual(innerEl, innerAl, message);
                    return (innerEl != null && innerAl != null);
                });
                checkLine();                                                 //-----------------
                checkLine();                                                 // <auto-generated>
                checkLine();                                                 //     This code was generated by a tool.
                lineNumber++; er.ReadLine(); ar.ReadLine(); // don't care    //     Runtime Version:4.0.30319.34209
                checkLine();                                                 //
                checkLine();                                                 //     Changes to this file may cause incorrect behavior and will be lost if
                checkLine();                                                 //     the code is regenerated.
                checkLine();                                                 // </auto-generated>
                var hasMoreLines = checkLine();                              //-----------------
                while (hasMoreLines)
                {
                    hasMoreLines = checkLine();
                }
                checkLine();
            }
        }

        [TestMethod]
        public void Generate()
        {
            var pathToOurAssembly = Assembly.GetExecutingAssembly().Location;
            var sourceAssembly = new FileInfo(pathToOurAssembly);

            var randomFileName = Path.GetRandomFileName();
            var tempPath = Path.Combine(Path.GetTempPath(), randomFileName);
            var tempDirectoryInfo = new DirectoryInfo(tempPath);
            tempDirectoryInfo.Create();

            try
            {
                var cut = new RestartableAddInGenerator(NamespaceName, sourceAssembly, tempDirectoryInfo);

                cut.Generate();

                var generatedFile = Path.Combine(tempDirectoryInfo.FullName, "RestartableThing.cs");
                var generatedFileInfo = new FileInfo(generatedFile);
                Assert.IsTrue(generatedFileInfo.Exists);

                var actual = File.ReadAllText(generatedFileInfo.FullName);
                AssertGeneratedLinesEqual(ExpectedRestartableThing, actual);
            }
            finally
            {
                try
                {
                    tempDirectoryInfo.Delete(true);
                }
                catch (IOException) { /* ignore */ }
            }
        }

        [TestMethod]
        public void GenerateFromTypeIThing()
        {
            var actual = RestartableAddInGenerator.GenerateFromType(NamespaceName, typeof(IThing));

            AssertGeneratedCode(ExpectedRestartableThing, actual);
        }

        [TestMethod]
        public void CreateCompileUnit()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void CreateNamespace()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();

            RestartableAddInGenerator.CreateNamespace(actual, NamespaceName);

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
}
";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void CreateClass()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();

            var ns = RestartableAddInGenerator.CreateNamespace(actual, NamespaceName);
            RestartableAddInGenerator.CreateClass(ns, ClassName, HostViewFullName);

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.HostViews.IThing>, Solink.HostViews.IThing
    {
    }
}
";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void CreateConstructor()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();

            CreateClassWithConstructor(actual);

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.HostViews.IThing>, Solink.HostViews.IThing
    {
        
        public RestartableThing(Solink.AddIn.Helpers.AddInFacade facade, System.AddIn.Hosting.AddInToken token, System.AddIn.Hosting.Platform platform) : 
                base(facade, token, platform)
        {
        }
    }
}
";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void CreateFactoryMethod()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();
            var @class = CreateClassWithConstructor(actual);

            RestartableAddInGenerator.CreateFactoryMethod(@class, HostViewFullName);

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.HostViews.IThing>, Solink.HostViews.IThing
    {
        
        public RestartableThing(Solink.AddIn.Helpers.AddInFacade facade, System.AddIn.Hosting.AddInToken token, System.AddIn.Hosting.Platform platform) : 
                base(facade, token, platform)
        {
        }
        
        public static Solink.HostViews.IThing Factory(Solink.AddIn.Helpers.AddInFacade facade, System.AddIn.Hosting.AddInToken token, System.AddIn.Hosting.Platform platform)
        {
            return new RestartableThing(facade, token, platform);
        }
    }
}
";
            AssertGeneratedCode(expected, actual);
        }

        private static CodeTypeDeclaration CreateClassWithConstructor(CodeCompileUnit actual)
        {
            var ns = RestartableAddInGenerator.CreateNamespace(actual, NamespaceName);
            var @class = RestartableAddInGenerator.CreateClass(ns, ClassName, HostViewFullName);
            RestartableAddInGenerator.CreateConstructor(@class);
            return @class;
        }

        [TestMethod]
        public void CreateFuncMethodForVoid()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();
            var ns = RestartableAddInGenerator.CreateNamespace(actual, NamespaceName);
            var @class = RestartableAddInGenerator.CreateClass(ns, ClassName, HostViewFullName);

            var methodParameters = new[]
            {
                Tuple.Create(typeof(IList<string>), "strings"),
            };
            RestartableAddInGenerator.CreateFuncMethod(@class, "AddToList", methodParameters, typeof(void));

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.HostViews.IThing>, Solink.HostViews.IThing
    {
        
        public void AddToList(System.Collections.Generic.IList<string> strings)
        {
            base.Action(_ => _.AddToList(strings));
        }
    }
}
";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void CreateFuncMethod()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();
            var ns = RestartableAddInGenerator.CreateNamespace(actual, NamespaceName);
            var @class = RestartableAddInGenerator.CreateClass(ns, ClassName, HostViewFullName);

            var methodParameters = new Tuple<Type, string>[]
            {
            };
            RestartableAddInGenerator.CreateFuncMethod(@class, "ComputeAnswerToLifeAndUniverseEverything", methodParameters, typeof(int));

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.HostViews.IThing>, Solink.HostViews.IThing
    {
        
        public int ComputeAnswerToLifeAndUniverseEverything()
        {
            return base.Func(_ => _.ComputeAnswerToLifeAndUniverseEverything());
        }
    }
}
";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void CreateProperty()
        {
            var actual = RestartableAddInGenerator.CreateCompileUnit();
            var ns = RestartableAddInGenerator.CreateNamespace(actual, NamespaceName);
            var @class = RestartableAddInGenerator.CreateClass(ns, ClassName, HostViewFullName);

            RestartableAddInGenerator.CreateProperty(@class, "Id", typeof(int), true);

            const string expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solink.Sample
{
    
    
    public sealed class RestartableThing : Solink.AddIn.Helpers.RestartableAddIn<Solink.HostViews.IThing>, Solink.HostViews.IThing
    {
        
        public int Id
        {
            get
            {
                return base.Func(_ => _.Id);
            }
            set
            {
                base.Action(_ => _.Id = value);
            }
        }
    }
}
";
            AssertGeneratedCode(expected, actual);
        }

        [TestMethod]
        public void GenerateClassNameIThing()
        {
            var actual = RestartableAddInGenerator.GenerateClassName("IThing");
            Assert.AreEqual("RestartableThing", actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenerateClassNameThing()
        {
            RestartableAddInGenerator.GenerateClassName("Thing");
        }
    }
}
