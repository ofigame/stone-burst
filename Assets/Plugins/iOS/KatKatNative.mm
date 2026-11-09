// TriKat iOS yerel yardımcıları (2026-11-09): dokunsal geri bildirim ve cihaz ülkesi.
// C# tarafı: KatKat.Game.Haptics, GameController.DeviceCountry. CİHAZDA DENENMEDİ.
#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>

static UIImpactFeedbackGenerator *katkatLight;
static UIImpactFeedbackGenerator *katkatMedium;

extern "C" {

void KatKat_Haptic(int strength)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (strength <= 0) {
            if (!katkatLight) katkatLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [katkatLight impactOccurred];
        } else {
            if (!katkatMedium) katkatMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            [katkatMedium impactOccurred];
        }
    });
}

// Dönen bellek Unity tarafından serbest bırakılır (IL2CPP string sözleşmesi: malloc ile ayrılmış C dizesi).
const char *KatKat_CountryCode()
{
    NSString *code = [[NSLocale currentLocale] objectForKey:NSLocaleCountryCode];
    if (code == nil) return NULL;
    const char *utf8 = [code UTF8String];
    char *copy = (char *)malloc(strlen(utf8) + 1);
    strcpy(copy, utf8);
    return copy;
}

}
