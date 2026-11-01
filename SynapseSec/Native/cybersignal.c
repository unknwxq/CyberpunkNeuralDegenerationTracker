// simple native c function to calculate operator humanity stability index (0 to 100)
#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif

EXPORT int calc_humanity_index(int stress_level, int ghz_load, int blocker_doses)
{
    if (stress_level < 0 || stress_level > 10 || ghz_load < 0 || blocker_doses < 0)
        return -1; // invalid input code

    // base humanity is 100, stress and ghz reduce it, immunoblockers restore +6 each
    int humanity = 100 - (stress_level * 6) - (ghz_load * 5) + (blocker_doses * 6);

    if (humanity < 0) return 0;
    if (humanity > 100) return 100;
    return humanity;
}