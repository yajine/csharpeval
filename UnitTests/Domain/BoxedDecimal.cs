namespace ExpressionEvaluator.UnitTests.Domain
{
    public class BoxedDecimal
    {
        public bool IsTriggered { get; set; }
        public bool CanWithdraw { get; set; }
        public decimal AmountToWithdraw { get; set; }
    }

    interface IVariable
    {
        TResult Valor<TResult>();
        string Codigo { get; }
        string Simbolo { get; }
        string SimboloExp { get; }
    }

    class Parametro : IVariable
    {
        private string _codigo;
        private dynamic _valor;
        private readonly string _simbolo;

        public string Codigo
        {
            get { return _codigo; }
        }

        public string Simbolo
        {
            get { return _simbolo; }
        }

        public string SimboloExp
        {
            get { return _simbolo + ".Valor<int>()"; }
        }

        public Parametro(string codigo, dynamic valor)
        {
            _codigo = codigo;
            _valor = valor;
            _simbolo = _codigo;
        }

        public TResult Valor<TResult>()
        {
            return (TResult)_valor;
        }

        public TResult Valor2<TResult>(TResult value) 
        {
            return value;
        }

        public object Valor3<TResult, TResult2>(int test, TResult2 value, TResult value2)
        {
            switch (test)
            {
                case 1:
                    return value;
                case 2:
                    return value2;
            }
            return null;
        }
    }

}