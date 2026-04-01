namespace прпгр.Models
{
    public class MaterialsFilterViewModel
    {
        public string Query { get; set; }           // общая строка поиска
        public string Subject { get; set; }
        public string Course { get; set; }
        public bool OnlyPremium { get; set; }
        public int? MinRating { get; set; }
    }
}
