namespace GIP.PRJ.TraiteurAppGip1.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderedOn { get; set; }
        public string TimeSlot { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public int Reduction { get; set; }
        public bool IsPaid { get; set; }

        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
