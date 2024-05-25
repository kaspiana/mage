namespace Mage.Engine;

public struct SemanticVersion {

    public int releaseType;
    public int major;
    public int minor;
    public int patch;

    public static string ToString(int releaseType, int major, int minor, int patch){
        var releaseTypeStr = "";
        switch(releaseType){
            default:
            case -1: releaseTypeStr = "alpha_"; break;
            case 0: releaseTypeStr = "beta_"; break;
        }
        return $"{releaseTypeStr}{major}.{minor}.{patch}";
    }

    public override string ToString()
    {
        return SemanticVersion.ToString(releaseType, major, minor, patch);
    }

    public SemanticVersion Normalise(){
        return new SemanticVersion(){
            releaseType = releaseType,
            major = major,
            minor = 0,
            patch = 0
        };
    }

    public static SemanticVersion FromString(string versionStr){
        int[] numericalParts;
        var semVer = new SemanticVersion(){
            releaseType = 1,
            major = 1,
            minor = 0,
            patch = 0
        };

        if(versionStr.Contains('_')){
            var parts = versionStr.Split('_');
            var releaseTypeStr = parts[0];
            var numericalStr = parts[1];
            numericalParts = numericalStr
                                .Split('.')
                                .Select(s => int.Parse(s))
                                .ToArray();

            if(releaseTypeStr == "alpha") semVer.releaseType = -1;
            else if(releaseTypeStr == "beta") semVer.releaseType = 0;

        } else {
            numericalParts = versionStr
                                .Split('.')
                                .Select(s => int.Parse(s))
                                .ToArray();
        }

        switch(numericalParts.Count()){
            case 0: break;
            case 1: 
                semVer.major = numericalParts[0]; break;
            case 2:
                semVer.major = numericalParts[0];
                semVer.minor = numericalParts[1]; break;
            case >= 3:
                semVer.major = numericalParts[0];
                semVer.minor = numericalParts[1]; 
                semVer.patch = numericalParts[2]; break;
        }

        return semVer;
    }

    public static bool operator ==(SemanticVersion lhs, SemanticVersion rhs){
        return lhs.releaseType == rhs.releaseType
                && lhs.major == rhs.major
                && lhs.minor == rhs.minor
                && lhs.patch == rhs.patch;
    }

    public static bool operator !=(SemanticVersion lhs, SemanticVersion rhs){
        return !(lhs == rhs);
    }

    public static bool operator <=(SemanticVersion lhs, SemanticVersion rhs){
        return (lhs == rhs) || (lhs < rhs);
    }

    public static bool operator >=(SemanticVersion lhs, SemanticVersion rhs){
        return (lhs == rhs) || (lhs > rhs);
    }

    public static bool operator <(SemanticVersion lhs, SemanticVersion rhs){
        if(lhs.releaseType > rhs.releaseType) return false;
        if(lhs.releaseType < rhs.releaseType) return true;

        if(lhs.major > rhs.major) return false;
        if(lhs.major < rhs.major) return true;

        if(lhs.minor > rhs.minor) return false;
        if(lhs.minor < rhs.minor) return true;

        return lhs.patch < rhs.patch;
    }

    public static bool operator >(SemanticVersion lhs, SemanticVersion rhs){
        if(lhs.releaseType < rhs.releaseType) return false;
        if(lhs.releaseType > rhs.releaseType) return true;

        if(lhs.major < rhs.major) return false;
        if(lhs.major > rhs.major) return true;

        if(lhs.minor < rhs.minor) return false;
        if(lhs.minor > rhs.minor) return true;

        return lhs.patch > rhs.patch;
    }
}