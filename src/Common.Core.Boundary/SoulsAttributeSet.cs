using CSharpFunctionalExtensions;

namespace Common.Core.Boundary
{
    public class SoulsAttributeSet : ValueObject
    {
        public SoulsAttributeSet(
            decimal maxHealth = 0,
            decimal maxMana = 0,
            decimal maxStamina = 0,
            decimal physicalPower = 0,
            decimal physicalDefense = 0)
        {
            MaxHealth = maxHealth;
            MaxMana = maxMana;
            MaxStamina = maxStamina;
            PhysicalPower = physicalPower;
            PhysicalDefense = physicalDefense;
        }

        private SoulsAttributeSet() { }

        public decimal MaxHealth { get; private set; }

        public decimal MaxMana { get; private set; }

        public decimal MaxStamina { get; private set; }

        public decimal PhysicalPower { get; private set; }

        public decimal PhysicalDefense { get; private set; }

        protected override IEnumerable<IComparable> GetEqualityComponents()
        {
            return [
                this.MaxHealth,
                this.MaxMana,
                this.MaxStamina,
                this.PhysicalPower,
                this.PhysicalDefense,
            ];
        }
    }
}
