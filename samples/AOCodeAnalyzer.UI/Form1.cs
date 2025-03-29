using AOCodeAnalyzer.TestGenerator.CSharpAnalyzer.Services;
using AOCodeAnalyzer.TestGenerator.TypeScriptAnalyzer.Services;

namespace AOCodeAnalyzer.UI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var parser = new TSCodeParserService();
            var testSuggestions = parser.ParseCode(richTextBox1.Text);
            var generator = new TSTestGeneratorService();
            richTextBox2.Text  = generator.GenerateTests(testSuggestions);


        }
    }
}
