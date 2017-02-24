# Modders Notes

### Incompatabilities:
> This mod detours the original Verse.Pawn_HealthTracker.CheckForStateChange, and will therefore be incompatable with any other mod that attempts to edit this file.

### Utilising LessThanLethal:
> To utilise the LessThanLethal system, and produce your own mods that can work aside LessThanLethal is very simple.  
>  
> Simply ensure your DamageDefs and HediffDefs defNames begin with `"LTL_"`*, as seen in `./DamageDefs/Damages_GlobalInjury.xml`
