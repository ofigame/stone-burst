# Renk, boyalı alan ve malzeme

PNG olmayan bütün düz/gradient alanlarda aşağıdaki sRGB HEX değerleri kullanılır. JSON referansı olmayan yeni rastgele renk üretilmez. Görselde boyalı taş yüzleri bu paletle yeniden renklendirilmez: mevcut sprite/shader aynen kalır.

| Token | HEX |
|---|---|
| ink | #203846 |
| inkSecondary | #53646D |
| inkMuted | #66747B |
| ivory | #FFF3DE |
| ivoryTop | #FFF7E8 |
| ivoryBottom | #F1DFC0 |
| sand | #D8C09C |
| brass | #B99760 |
| rimHighlight | #FFF9ED |
| rimShadow | #8E785C |
| teal | #118C83 |
| tealTop | #1BA99E |
| tealBottom | #08766F |
| tealPressed | #06645E |
| tealDisabled | #728B86 |
| onTeal | #FFF9ED |
| secondaryTop | #FFF6E6 |
| secondaryBottom | #EAD7B8 |
| secondaryPressed | #DCC6A2 |
| coral | #CC6956 |
| coralDark | #934B3E |
| purple | #685680 |
| purpleDark | #42374F |
| success | #32866D |
| warning | #AD7838 |
| error | #A44E43 |
| track | #D9CEB9 |
| divider | #D6C6AD |
| scrim | #111C24 |
| shadow | #302C2A |
| homeTop | #697F73 |
| homeBottom | #435E55 |
| tabUnselected | #F0E6D3 |
| tabSelected | #147E75 |
| lockedVeil | #5E6C70 |
| focus | #244E5A |

## Boyama reçeteleri

Panel: dikey gradient ivoryTop→ivoryBottom; stop0 ve1; 4px rimHighlight,3px brass,12px ivory pah,9px sand yan yüz. Modal crest yalnız32px yükselen kontur; mascot max196×132; zafer yıldızları gerçekten kazanılan sayıda, büyük bebek karakter/çiçek/fiyonk yok. Kartın her boş alanına ikon eklenmez.

Ana sayfa üst yüzeyi homeTop→homeBottom dikey gradient; y0..312. Logo360×108; yıldızkapsülü208×88; bölgeplakası984×120. Bölgeplakası ivoryTop→ivoryBottom; alt gölge offset(0,8),blur14,alpha0.20. Altmenu tabUnselected, seçili kapsül tabSelected ve yazı onTeal. Coğrafi harita üzerine zemin gradienti/çizgi basılmaz.

Primary: tealTop→tealBottom dikey gradient, stop0/1; 3pxbrass; iç üst yansıma2px onTeal alpha0.32; shadow(0,6),blur10,alpha0.20. Secondary: secondaryTop→secondaryBottom, inkmetin, aynıkenar. Pressed sabit tealPressed veya secondaryPressed, içeriği3px aşağı, shadowy2. Scale1; yeni tıklama/bekleme davranışı yok. Disabled tealDisabled alpha0.62; metin alpha0.72; mevcut interactable kuralı korunur. Focusring3px focus, dışarı6px; herkontrol keyboard-navigation davranışına müdahale etmeden.

Progress track track renginde, fill teal; 14yükseklik, radius7; sayı sağa dayalı ayrı TMP. Track üstüne dekor çerçeve yok. Ayırıcı divider/1px/alpha0.65; gövdede divider satırlar arasında yalnız birkez.

## Karartma

Bütün modalların altında tüm fiziksel ekranı kapsayan düz #111C24, alpha0.72; SafeArea dışı, boş sütunlar ve tahta delikleri de karanlık. Panel, yazı ve modal kontrolü scrim üstünde. Altta kalan harita/HUD/düğme etkileşimi engelli. Dışa dokunma mevcut eylemi korur: yeni close davranışı eklenmez. İkinci modalda toplam alpha0.82 için ikinci scrim alpha=(0.82-0.72)/(1-0.72)=0.3571429; iki kez0.72 uygulanmaz. Tutorial #111C24alpha0.76; sadece mevcut spotlight delikleri açık. Metin kartına karartma uygulanmaz.
