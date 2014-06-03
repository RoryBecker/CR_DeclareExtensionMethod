CR_DeclareExtensionMethod
=========================

A simple plugin to help you declare extension methods quickly.

Once installed, start with the following code which attempts to call **MyNewMethod** which of course does not exist. 

	public class Data { }
    public class App
    {
        private void AMethod()
        {
            Data data;
            data.MyNewMethod(1, 2, 3);
        }
    }

 - Place your caret on the call to MyNewMethod
 - Hit your CodeRush \ Refactor key (defaults to Ctrl+`)
 - Choose **Declare Extension Method** from the menu. 

CodeRush generates a static class containing the stub of your new extension method.

	public static class DataExt
    {
        public static void MyNewMethod(this Data Source, int Param1, int Param2, int Param3)
        {
        }
    }

