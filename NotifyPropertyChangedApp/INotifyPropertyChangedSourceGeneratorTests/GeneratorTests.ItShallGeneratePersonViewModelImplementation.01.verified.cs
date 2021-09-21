//HintName: PersonViewModel.gen.cs

namespace Foo
{
public partial class PersonViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    private string FirstNameBackingField = null!;
    public string FirstName
    {
        get
        {
            return this.FirstNameBackingField;
        }
        set
        {
            this.FirstNameBackingField = value;
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(FirstName)));
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    private string LastNameBackingField = null!;
    public string LastName
    {
        get
        {
            return this.LastNameBackingField;
        }
        set
        {
            this.LastNameBackingField = value;
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(LastName)));
        }
    }

}

}
