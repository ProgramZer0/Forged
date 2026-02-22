public enum PhaseType
{
    NONE,
    Heating, //1
    Condensing, //2
    Shaping, //3
    Bloomery, //4
    AnvilHammering, //5
    Melting, //6
    Casting, //7
    Curing, //8
    Mixing, //9
    Exhausting, //10
    Moisturizing, //11
    SunExhaust //12
}

[System.Serializable]
public class Recipe 
{
    public string recipeID;

    public int[] inputItemIDs;

    public PhaseType phaseNeeded;

    public float requiredValue;

    public int outputItemID;
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

    Heating, //1
    Condensing, //2
    Shaping, //3
    Bloomery, //4
    AnvilHammering, //5
    Melting, //6
    Casting, //7
    Curing, //8
    Mixing, //9
    Exhausting, //10
    Moisturizing, //11
    SunExhaust //12


    bloom to metal

     */

}