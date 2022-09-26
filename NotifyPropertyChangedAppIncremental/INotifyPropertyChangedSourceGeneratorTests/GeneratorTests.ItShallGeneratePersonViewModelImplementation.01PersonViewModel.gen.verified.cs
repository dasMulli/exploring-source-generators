//HintName: PersonViewModel.gen.cs

namespace Foo
{
public partial class PersonViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

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
            if (value != this.FirstNameBackingField)
            {
                this.FirstNameBackingField = value;
                this.OnPropertyChanged(nameof(FirstName));
            }
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
            if (value != this.LastNameBackingField)
            {
                this.LastNameBackingField = value;
                this.OnPropertyChanged(nameof(LastName));
            }
        }
    }

}

}
