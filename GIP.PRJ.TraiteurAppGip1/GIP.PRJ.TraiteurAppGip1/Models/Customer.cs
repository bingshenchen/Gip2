namespace GIP.PRJ.TraiteurAppGip1.Models
{
    public enum CustomerRating
    { 
        A,
        B
    }
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CustomerRating Rating { get; set; }
        public string Info { get; set; }
        public string EmailAddress { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
