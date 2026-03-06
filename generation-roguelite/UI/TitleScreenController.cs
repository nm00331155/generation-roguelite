using Godot;

namespace GenerationRoguelite.UI;

public partial class TitleScreenController : Control
{
    [Signal]
    public delegate void StartPressedEventHandler();

    [Signal]
    public delegate void NewGamePressedEventHandler();

    [Signal]
    public delegate void ContinuePressedEventHandler();

    [Signal]
    public delegate void OpenFamilyTreePressedEventHandler();

    [Signal]
    public delegate void OpenCollectionPressedEventHandler();

    [Signal]
    public delegate void OpenSettingsPressedEventHandler();

    [Export]
    public NodePath NewGameButtonPath { get; set; } = "VBox/NewGameButton";

    [Export]
    public NodePath ContinueButtonPath { get; set; } = "VBox/ContinueButton";

    [Export]
    public NodePath FamilyTreeButtonPath { get; set; } = "VBox/MidButtons/FamilyTreeButton";

    [Export]
    public NodePath CollectionButtonPath { get; set; } = "VBox/MidButtons/CollectionButton";

    [Export]
    public NodePath SettingsButtonPath { get; set; } = "VBox/SettingsButton";

    private Button _newGameButton = null!;
    private Button _continueButton = null!;
    private Button _familyTreeButton = null!;
    private Button _collectionButton = null!;
    private Button _settingsButton = null!;

    public override void _Ready()
    {
        _newGameButton = GetNode<Button>(NewGameButtonPath);
        _continueButton = GetNode<Button>(ContinueButtonPath);
        _familyTreeButton = GetNode<Button>(FamilyTreeButtonPath);
        _collectionButton = GetNode<Button>(CollectionButtonPath);
        _settingsButton = GetNode<Button>(SettingsButtonPath);

        _newGameButton.Pressed += HandleNewGamePressed;
        _continueButton.Pressed += HandleContinuePressed;
        _familyTreeButton.Pressed += HandleFamilyTreePressed;
        _collectionButton.Pressed += HandleCollectionPressed;
        _settingsButton.Pressed += HandleSettingsPressed;

        SetHasSaveData(false);
    }

    public override void _ExitTree()
    {
        _newGameButton.Pressed -= HandleNewGamePressed;
        _continueButton.Pressed -= HandleContinuePressed;
        _familyTreeButton.Pressed -= HandleFamilyTreePressed;
        _collectionButton.Pressed -= HandleCollectionPressed;
        _settingsButton.Pressed -= HandleSettingsPressed;
    }

    public void SetHasSaveData(bool hasSaveData)
    {
        _continueButton.Visible = hasSaveData;
        _familyTreeButton.Visible = hasSaveData;
        _collectionButton.Visible = hasSaveData;
    }

    private void HandleNewGamePressed()
    {
        EmitSignal(SignalName.NewGamePressed);
        EmitSignal(SignalName.StartPressed);
    }

    private void HandleContinuePressed()
    {
        EmitSignal(SignalName.ContinuePressed);
    }

    private void HandleFamilyTreePressed()
    {
        EmitSignal(SignalName.OpenFamilyTreePressed);
    }

    private void HandleCollectionPressed()
    {
        EmitSignal(SignalName.OpenCollectionPressed);
    }

    private void HandleSettingsPressed()
    {
        EmitSignal(SignalName.OpenSettingsPressed);
    }
}
