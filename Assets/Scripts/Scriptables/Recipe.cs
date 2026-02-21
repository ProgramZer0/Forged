public enum PhaseType
{
    NONE,
    Heating, //1
    Condensing, //2
    Bloomery, //3
    AnvilHammering, //4
    Melting, //5
    Casting, //6
    Curing, //7
    Mixing, //8
    Exhausting, //9
    Moisturizing, //10
    SunExhaust //11
}

[System.Serializable]
public class Recipe 
{
    public string recipeID;

    public int[] inputItemIDs;

    public PhaseType phaseNeeded;

    public float requiredValue;

    /*
     * 	heat level (1-10 for heat lvl),
	 *  condencing (% to condence),
     *  bloomery (ratio of charcoal),
     *  anvil hammering (swings),
     *  melting (1-10 for heat lvl),
     *  casting (durabilty of cast 1-3),
     *  curing (1-10 for heat lvl), //15 seconds per heat level
     *  mixing (1-99), //mixing id that defines what needs to be mixed 
     *  exahusting (1-10 for heat lvl),
     *  moisterizing (time in seconds),
     *  Sun_exhaust (time in seconds)
     *  
     *
     */

    public int outputItemID;
}