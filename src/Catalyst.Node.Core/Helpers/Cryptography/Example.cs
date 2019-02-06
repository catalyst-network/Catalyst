public class Example{

    public void DoStuff(){
        var context = new NSecCryptoContext();
        var key = context.GenerateKey();
        context.Sign(key);


        ICryptoContext contextI = context;
        var keyI = contextI.GenerateKey();
        contextI.Sign(keyI);

    }

}