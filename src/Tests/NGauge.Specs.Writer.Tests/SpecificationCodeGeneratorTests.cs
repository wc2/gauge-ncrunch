using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NGauge.Specs.Writer.Providers;
using NSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace NGauge.Specs.Writer.Tests
{
    public sealed class SpecificationCodeGeneratorTests
    {
        public static readonly object[] CodeSafeNameTestCases =
        {
            new[] {"Somename", "Somename"},
            new[] {"Some name", "Somename"},
            new[] {"Some name!", "Somename"},
            new[] {"Test: Something", "TestSomething"},
            new[] {"Should [remove] non-word characters", "Shouldremovenonwordcharacters"}
        };

        [Fact]
        public void ctor_GeneratedCodeNamespaceProviderRequired()
        {
            Assert.Throws<ArgumentNullException>(
                "generatedCodeNamespaceProvider",
                () => new SpecificationCodeGenerator(
                    null,
                    Substitute.For<IGetInvariantTestAttributor>()));
        }

        [Fact]
        public void ctor_IGetInvariantTestAttributorRequired()
        {
            Assert.Throws<ArgumentNullException>(
                "getInvariantTestAttributor",
                () => new SpecificationCodeGenerator(
                    Substitute.For<IGeneratedCodeNamespaceProvider>(),
                    null));
        }

        [Fact]
        public void GenerateCode_SpecificationRequired()
        {
            var generator = CreateSpecificationCodeGenerator();

            Assert.Throws<ArgumentNullException>(
                "specification",
                () => generator.GenerateCode(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GenerateCode_SpecificationNameRequired(string name)
        {
            var generator = CreateSpecificationCodeGenerator();

            Assert.Throws<ArgumentException>(
                "specification",
                () => generator.GenerateCode(GetMockSpecification(name)));
        }

        [Theory, AutoData]
        public void GenerateCode_HasSingleNamespaceWithExpectedName(string expectedNamespace)
        {
            var generatedCodeNamespaceProvider = Substitute.For<IGeneratedCodeNamespaceProvider>();
            generatedCodeNamespaceProvider
                .GetNamespace()
                .Returns(expectedNamespace);
            var generator =
                CreateSpecificationCodeGenerator(generatedCodeNamespaceProvider: generatedCodeNamespaceProvider);

            var generatedCode = generator.GenerateCode(GetMockSpecification());

            GetNamespace(generatedCode)
                .Name
                .Should()
                .Be(expectedNamespace);
        }

        [Fact]
        public void GenerateCode_NamespaceShouldImportRunner()
        {
            var generator = CreateSpecificationCodeGenerator();

            var generatedCode = generator.GenerateCode(GetMockSpecification());

            GetNamespace(generatedCode)
                .Imports[0]
                .Namespace
                .Should()
                .Be("NGauge.Runner");
        }

        [Fact]
        public void GenerateCode_NamespaceShouldHaveSingleClass()
        {
            var generator = CreateSpecificationCodeGenerator();

            var generatedCode = generator.GenerateCode(GetMockSpecification());

            GetTestClass(generatedCode)
                .IsClass
                .Should()
                .Be(true);
        }

        [Theory]
        [MemberData(nameof(CodeSafeNameTestCases))]
        public void GenerateCode_ClassShouldHaveExpectedName(string name, string expectedName)
        {
            var specification = Substitute.For<ISpecification>();
            specification
                .Name
                .Returns(name);

            var generator = CreateSpecificationCodeGenerator();

            var generatedCode = generator.GenerateCode(specification);

            GetTestClass(generatedCode)
                .Name
                .Should()
                .Be(expectedName);
        }

        [Fact]
        public void GenerateCode_ClassShouldHaveOneMethodForEachScenario()
        {
            const int sampleSize = 5;
            var generator = CreateSpecificationCodeGenerator();

            var generatedCode = generator.GenerateCode(
                GetMockSpecification(scenarios: GetSample<IScenario>(sampleSize)));

            GetTestMethods(generatedCode)
                .Count()
                .Should()
                .Be(sampleSize);
        }

        [Theory]
        [MemberData(nameof(CodeSafeNameTestCases))]
        public void GenerateCode_EachScenarioMethodShouldHaveSameNameAsScenario(string name, string expectedName)
        {
            var generator = CreateSpecificationCodeGenerator();
            var generatedCode = generator.GenerateCode(
                GetMockSpecification(scenarios: new[] {GetMockScenario(name)}));

            GetTestMethods(generatedCode)
                .Select(method => method.Name)
                .ShouldBeEquivalentTo(new[] {expectedName});
        }

        [Fact]
        public void GenerateCode_EachScenarioHasInvariantTestAttribute()
        {
            var getInvariantTestAttributor = Substitute.For<IGetInvariantTestAttributor>();
            getInvariantTestAttributor
                .GetAttribute()
                .Returns(typeof(FactAttribute));
            var generator = CreateSpecificationCodeGenerator(getInvariantTestAttributor: getInvariantTestAttributor);

            var generatedCode = generator.GenerateCode(
                GetMockSpecification(scenarios: GetSample<IScenario>()));

            GetTestMethods(generatedCode)
                .All(HasExpectedTestInvariantAttribute<FactAttribute>)
                .Should()
                .Be(true);
        }

        [Fact]
        public void GenerateCode_ScenarioCreatesRunner()
        {
            var generator = CreateSpecificationCodeGenerator();

            var generatedCode = generator.GenerateCode(
                GetMockSpecification(scenarios: GetSample<IScenario>()));

            //ExpectedMethodShouldBeCalled(
            //    GetTestStatements(generatedCode).FirstOrDefault(),
            //    typeof(Runner.Scenario).FullName,
            //    nameof(Runner.Scenario.CreateRunner));
        }

        private static ISpecificationCodeGenerator CreateSpecificationCodeGenerator(
            IGeneratedCodeNamespaceProvider generatedCodeNamespaceProvider = null,
            IGetInvariantTestAttributor getInvariantTestAttributor = null)
        {
            return new SpecificationCodeGenerator(
                generatedCodeNamespaceProvider ?? Substitute.For<IGeneratedCodeNamespaceProvider>(),
                getInvariantTestAttributor     ?? GetMockGetInvariantTestAttributor());
        }

        private static IGetInvariantTestAttributor GetMockGetInvariantTestAttributor()
        {
            var getInvariantTestAttributor = Substitute.For<IGetInvariantTestAttributor>();

            getInvariantTestAttributor
                .GetAttribute()
                .Returns(Substitute.For<Type>());

            return getInvariantTestAttributor;
        }

        private static ISpecification GetMockSpecification(string name = "some name", IEnumerable<IScenario> scenarios = null)
        {
            var specification = Substitute.For<ISpecification>();

            specification
                .Name
                .Returns(name);
            specification
                .Scenarios
                .Returns(scenarios ?? Enumerable.Empty<IScenario>());

            return specification;
        }

        private static IScenario GetMockScenario(string name)
        {
            var scenario = Substitute.For<IScenario>();

            scenario
                .Name
                .Returns(name);

            return scenario;
        }

        private static IEnumerable<T> GetSample<T>(int sampleSize = 3)
            where T : class
        {
            return Enumerable.Range(0, sampleSize).Select(_ => Substitute.For<T>());
        }

        private static CodeNamespace GetNamespace(CodeCompileUnit generatedCode)
        {
            return generatedCode.Namespaces[0];
        }

        private static CodeTypeDeclaration GetTestClass(CodeCompileUnit generatedCode)
        {
            return GetNamespace(generatedCode).Types[0];
        }

        private static IEnumerable<CodeMemberMethod> GetTestMethods(CodeCompileUnit generatedCode)
        {
            return GetTestClass(generatedCode)
                .Members
                .OfType<CodeMemberMethod>();
        }

        private static IEnumerable<CodeStatement> GetTestStatements(CodeCompileUnit generatedCode)
        {
            return GetTestMethods(generatedCode)
                .FirstOrDefault()
                ?.Statements
                .Cast<CodeStatement>();
        }

        private static bool HasExpectedTestInvariantAttribute<T>(CodeTypeMember testMethod)
        {
            return testMethod.CustomAttributes[0].Name == typeof(T).FullName;
        }

        private static void ExpectedMethodShouldBeCalled(CodeMethodInvokeExpression invokeExpression, string expectedTypeName, string expectedMethodName)
        {
            invokeExpression
                .Method
                .MethodName
                .Should()
                .Be(expectedMethodName);
        }
    }
}