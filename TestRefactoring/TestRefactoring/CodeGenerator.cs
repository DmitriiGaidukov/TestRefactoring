namespace TestRefactoring
{
    public class CodeGenerator
    {
        public static string GetIntegrationTestCode(string typeName, string typeNamespace, string testTypeName, string testTypeNamespace)
        {

            var code = 
$@"using NUnit.Framework;                    
using {typeNamespace};
                    
namespace {testTypeNamespace}
{{                    
    [TestFixture]
    public class {testTypeName} : TestBase
    {{
        public {typeName} {typeName} {{ get; set; }}

        [OneTimeSetUp]
        public void Init()
        {{
            Container.InjectProperties(this);
        }}

        [Test]
        public void SomeTest()
        {{
            // --============================================= arrangement

            // --============================================= action
            //{typeName}.

            // --============================================= assertion
        }}
    }}

}}";

            return code;
        }

        public static string GetUnitTestCode(string typeName, string typeNamespace, string testTypeName, string testTypeNamespace)
        {
            var code =
$@"using NUnit.Framework;                    
using {typeNamespace};
using Core.Fakes;
                    
namespace {testTypeNamespace}
{{                    
    [TestFixture]
    public class {testTypeName} : TestFakeBase
    {{
        public {typeName} {typeName} {{ get; set; }}

        [SetUp]
        public void Init()
        {{
            Container.InjectProperties(this);
        }}

        [Test]
        public void SomeTest()
        {{
            // --============================================= arrangement
            //Container.SetTable();

            // --============================================= action
            //{typeName}.

            // --============================================= assertion
        }}
    }}

}}";

            return code;
        }
    }
}