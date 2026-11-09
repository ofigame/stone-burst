// TriKat iOS yerel yardımcıları (2026-11-09): dokunsal geri bildirim ve cihaz ülkesi.
// C# tarafı: KatKat.Game.Haptics, GameController.DeviceCountry. CİHAZDA DENENMEDİ.
#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

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
// App Tracking Transparency (2026-11-09): reklamlar başlamadan önce sistem izin penceresi. Sonuç ana iş
// parçacığında geri çağrıyla döner: 0 belirlenmedi, 1 kısıtlı, 2 reddedildi, 3 izin verildi. iOS 14 öncesi: 3.
typedef void (*KatKatTrackingCallback)(int status);

void KatKat_RequestTracking(KatKatTrackingCallback cb)
{
    if (@available(iOS 14, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            dispatch_async(dispatch_get_main_queue(), ^{ if (cb) cb((int)status); });
        }];
    } else {
        if (cb) cb(3);
    }
}

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
