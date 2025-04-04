using Paramore.Darker;
using SampleMauiTestApp.QueryHandlers;

namespace SampleMauiTestApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IQueryProcessor _queryProcessor;
        public MainPage(IQueryProcessor queryProcessor)
        {
            InitializeComponent();
            this._queryProcessor = queryProcessor;
        }

        private void OnPersonButtonClicked(object sender, EventArgs e)
        {
            if (int.TryParse(numberEntry.Text, out int number))
            {
                ;
                var person = _queryProcessor.ExecuteAsync(new GetPersonNameQuery(number)).Result;
                
                resultLabel.Text = $"Person: {person}";
            }
            else
            {
                resultLabel.Text = "Please enter a person ID.";
            }
        }

        private void OnAllPeopleButtonClicked(object sender, EventArgs e)
        {
            var people = _queryProcessor.ExecuteAsync(new GetPeopleQuery()).Result;
            
            resultLabel.Text = "People:\n" + string.Join("\n", people.Select(p => $"  {p.Key}:\t{p.Value}"));
        }
    }
}
