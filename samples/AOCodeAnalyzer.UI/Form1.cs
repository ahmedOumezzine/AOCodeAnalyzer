using AOCodeAnalyzer.TestGenerator.CSharpAnalyzer.Services;

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
            var parser = new CSharpCodeParserService();
            var testSuggestions = parser.ParseCode(richTextBox1.Text);
            var generator = new CSharpTestGeneratorService();
            richTextBox2.Text  = generator.GenerateTests(testSuggestions);


        }
    }
}
