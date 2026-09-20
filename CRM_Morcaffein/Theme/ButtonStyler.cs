namespace CRM_Morcaffein.Theme
{
    /// <summary>
    /// FlatStyle.Flat buttons don't repaint their fill when Enabled flips false — they just
    /// grey the text and keep whatever BackColor was set, so a disabled primary button still
    /// looks like a brand-colored, seemingly-clickable button. Call TrackEnabledState once
    /// (after the button's "active" colors are set) on any button whose Enabled is toggled at
    /// runtime, and it'll swap to the neutral disabled palette automatically.
    /// </summary>
    public static class ButtonStyler
    {
        public static void TrackEnabledState(Button button)
        {
            var activeBackColor = button.BackColor;
            var activeForeColor = button.ForeColor;

            void Apply()
            {
                button.BackColor = button.Enabled ? activeBackColor : BrandPalette.ButtonDisabledFill;
                button.ForeColor = button.Enabled ? activeForeColor : BrandPalette.ButtonDisabledText;
            }

            button.EnabledChanged += (s, e) => Apply();
            Apply(); // the button may already be disabled at hook-up time (e.g. designer default)
        }
    }
}
