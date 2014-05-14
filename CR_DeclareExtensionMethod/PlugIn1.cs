using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.CodeRush.Core;
using DevExpress.CodeRush.PlugInCore;
using DevExpress.CodeRush.StructuralParser;

namespace CR_DeclareExtensionMethod
{
    public partial class PlugIn1 : StandardPlugIn
    {
        // DXCore-generated code...
        #region InitializePlugIn
        public override void InitializePlugIn()
        {
            base.InitializePlugIn();
            registerDeclareExtensionMethod();
        }
        #endregion
        #region FinalizePlugIn
        public override void FinalizePlugIn()
        {
            //
            // TODO: Add your finalization code here.
            //

            base.FinalizePlugIn();
        }
        #endregion
        public void registerDeclareExtensionMethod()
        {
            DevExpress.CodeRush.Core.CodeProvider DeclareExtensionMethod = new DevExpress.CodeRush.Core.CodeProvider(components);
            ((System.ComponentModel.ISupportInitialize)(DeclareExtensionMethod)).BeginInit();
            DeclareExtensionMethod.ProviderName = "DeclareExtensionMethod"; // Should be Unique
            DeclareExtensionMethod.DisplayName = "Declare Extension Method";
            DeclareExtensionMethod.CheckAvailability += DeclareExtensionMethod_CheckAvailability;
            DeclareExtensionMethod.Apply += DeclareExtensionMethod_Apply;
            ((System.ComponentModel.ISupportInitialize)(DeclareExtensionMethod)).EndInit();
        }
        private void DeclareExtensionMethod_CheckAvailability(Object sender, CheckContentAvailabilityEventArgs ea)
        {

            // Get reference to Method
            MethodReferenceExpression MethodReferenceExpression = ea.Element as MethodReferenceExpression;
            if (MethodReferenceExpression == null)
                return;

            // Require: Method is qualified by an Object.
            // Get Object Reerence
            if (MethodReferenceExpression.Nodes.Count == 0)
                return;
            ElementReferenceExpression TheObject = MethodReferenceExpression.Nodes[0] as ElementReferenceExpression;
            if (TheObject == null)
                return;

            // Require: Method does not exist on Object class.
            // Require: Method does not exist as Extension Method.
            Method TheMethod = MethodReferenceExpression.GetDeclaration() as Method;
            if (TheMethod != null)
                return; // Method already Exists

            // Require: Class method is alledgedly on is not static.
            Class TheClass = TheObject.GetDeclaration() as Class;
            if (TheClass != null && TheClass.IsStatic)
                return; // Static classes can't have extension methods

            Struct TheStruct = TheObject.GetDeclaration() as Struct;
            if (TheStruct != null && TheStruct.IsStatic)
                return; // Static Structures can't have extension methods

            ea.Available = true;
        }

        private void DeclareExtensionMethod_Apply(Object sender, ApplyContentEventArgs ea)
        {
            using (ea.TextDocument.NewCompoundAction("Declare Extension Method"))
            {
                // Get reference to Method
                MethodReferenceExpression MethodReferenceExpression = ea.Element as MethodReferenceExpression;
                MethodCallExpression MethodCallExpression = MethodReferenceExpression.Parent as MethodCallExpression;
                MethodCall MethodCall = MethodReferenceExpression.Parent as MethodCall;
                LanguageElement TheElement = MethodCallExpression == null ? (LanguageElement)MethodCall : (LanguageElement)MethodCallExpression;
                IWithArguments WithArgs = TheElement as IWithArguments;

                var Builder = ea.NewElementBuilder();

                // NEW CLASS
                ElementReferenceExpression TheObject = MethodReferenceExpression.Nodes[0] as ElementReferenceExpression;
                Variable variable = TheObject.GetDeclaration() as Variable;
                string TheObjectClassName = (variable.DetailNodes[0] as TypeReferenceExpression).Name;
                string ClassName = TheObjectClassName + "Ext";
                var NewClass = new Class(ClassName);
                NewClass.IsStatic = true;
                NewClass.Visibility = MemberVisibility.Public;

                // NEW METHOD
                string ReturnTypeName;
                if (MethodCallExpression != null)
                {
                    ReturnTypeName = GetMethodReturnType((MethodCallExpression)MethodReferenceExpression.Parent);
                }
                else
                {
                    ReturnTypeName = "";
                }
                var NewMethod = Builder.BuildMethod(ReturnTypeName, MethodReferenceExpression.Name);
                NewMethod.IsStatic = true;
                NewMethod.Visibility = MemberVisibility.Public;

                // Add Source Param
                NewMethod.Parameters.Add(new ExtensionMethodParam(TheObjectClassName, "Source"));

                // Add Original Params
                int index = 0;
                foreach (Expression expression in WithArgs.Args)
                {
                    index += 1;
                    NewMethod.Parameters.Add(expression.ToParameter("Param" + index));
                }

                // Put Everything Together
                NewClass.AddNode(NewMethod);
                LanguageElement ParentNamespace = TheElement.GetParent(LanguageElementType.Namespace);
                ParentNamespace.AddNode(NewClass);

                // Insert new Code into Document
                SourceRange NSRange = ParentNamespace.GetFullBlockRange();
                SourcePoint InsertPoint = new SourcePoint(NSRange.End.Line, 1);
                SourceRange FormatRange = ea.TextDocument.InsertText(InsertPoint, CodeRush.Language.GenerateElement(NewClass));

                ea.TextDocument.Format(FormatRange);
            }
        }
        private string GetMethodReturnType(MethodCallExpression expression)
        {
            switch (expression.Parent.ElementType)
            {
                case LanguageElementType.MethodCall:
                    var MethodCall = expression.Parent as MethodCall;
                    var index = MethodCall.Arguments.IndexOf(expression);

                    return GetTypeOfNthParamOfMethod(index, MethodCall);
                case LanguageElementType.MethodCallExpression:
                    var MethodCallExpression = expression.Parent as MethodCallExpression;
                    var MCEindex = MethodCallExpression.Arguments.IndexOf(expression);
                    return GetTypeOfNthParamOfMethod(MCEindex, MethodCallExpression);

                    //var MCEDeclaration1 = expression.GetDeclaration() as Method;
                    //var MCEParam = MCEDeclaration1.Parameters[MCEindex] as Param;
                    //return MCEParam.GetTypeName();

                case LanguageElementType.InitializedVariable:
                    return GetTypeOfInitializedVariable(expression.Parent as InitializedVariable);
                case LanguageElementType.Assignment:
                    return GetTypeOfAssignment(expression.Parent as Assignment);
                default:
                    throw new Exception("");
            }
        }
        private static string GetTypeOfNthParamOfMethod(int ParamIndex, MethodCall MethodCall)
        {
            MethodReferenceExpression OuterMethodReference = (MethodCall.Nodes[0] as DevExpress.CodeRush.StructuralParser.MethodReferenceExpression);

            var Declaration1 = OuterMethodReference.GetDeclaration() as Method;
            // Previous line will return null if any parameter isn't explicitly calculable.
            if (Declaration1 == null)
                return ""; // Suggest void. Wrong, but best available option.

            var Param = Declaration1.Parameters[ParamIndex] as Param;
            return Param.GetTypeName();
        }
        private static string GetTypeOfNthParamOfMethod(int ParamIndex, MethodCallExpression MethodCallExpression)
        {
            MethodReferenceExpression OuterMethodReference = (MethodCallExpression.Nodes[0] as DevExpress.CodeRush.StructuralParser.MethodReferenceExpression);

            var Declaration1 = OuterMethodReference.GetDeclaration() as Method;
            // Previous line will return null if any parameter isn't explicitly calculable.
            if (Declaration1 == null)
                return ""; // Suggest void. Wrong, but best available option.

            var Param = Declaration1.Parameters[ParamIndex] as Param;
            return Param.GetTypeName();
        }
        private static string GetTypeOfInitializedVariable(InitializedVariable expression2)
        {
            return (expression2.DetailNodes[0] as TypeReferenceExpression).Name;
        }
        private static string GetTypeOfAssignment(Assignment assignment)
        {
            ElementReferenceExpression ERE = assignment.DetailNodes[0] as ElementReferenceExpression;
            var Declaration3 = (ERE.GetDeclaration() as IHasType).Type;
            return Declaration3.Name;
        }
    }
    public static class ExpressionExt
    {
        public static Param ToParameter(this Expression Expression, string Name)
        {
            return new Param(CodeRush.Refactoring.GetExpressionTypeFromContext(Expression), Name);
        }
    }
}
