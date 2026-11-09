# Kat Kat — Seviye Doğrulama Raporu

Üretildi: 2026-11-08 22:49 • Araç: tools/LevelLab (dotnet) • Sonuç: **1000/1000 geçti**

Statik kontrol (Bölüm 14; 1–20 ilk sürüm, 21+ ikinci aşama kapsamı) + kayıtlı çözümün gerçek `RulesEngine` ile tekrarı. Bu bir çözüm varlığı kanıtıdır; en kısa çözüm veya eğlence kanıtı değildir.

| Seviye | Statik | Çözüm adımı / limit | Dalga | Soyulan katman | Rulo (kazanılan/kullanılan) | Son hedefler | Sonuç |
|---|---|---|---|---|---|---|---|
| level_001 | OK | 1 / 3 | 2 | 16 | 0/0 | coral 8/8, yellow 4/4 | Won ✔ |
| level_002 | OK | 1 / 2 | 2 | 7 | 0/0 | blue 3/3 | Won ✔ |
| level_003 | OK | 2 / 4 | 3 | 17 | 0/0 | yellow 6/6, blue 8/4 | Won ✔ |
| level_004 | OK | 2 / 2 | 3 | 15 | 0/0 | blue 7/7 | Won ✔ |
| level_005 | OK | 3 / 3 | 5 | 28 | 0/0 | yellow 10/8 | Won ✔ |
| level_006 | OK | 2 / 6 | 5 | 38 | 0/0 | coral 19/19 | Won ✔ |
| level_007 | OK | 2 / 6 | 6 | 50 | 0/0 | blue 25/25 | Won ✔ |
| level_008 | OK | 6 / 12 | 11 | 62 | 0/0 | yellow 27/27 | Won ✔ |
| level_009 | OK | 3 / 7 | 5 | 31 | 0/0 | coral 14/12 | Won ✔ |
| level_010 | OK | 3 / 9 | 8 | 84 | 0/0 | blue 31/30 | Won ✔ |
| level_011 | OK | 4 / 9 | 9 | 53 | 0/0 | yellow 26/26 | Won ✔ |
| level_012 | OK | 3 / 8 | 5 | 33 | 0/0 | coral 15/14 | Won ✔ |
| level_013 | OK | 3 / 7 | 7 | 44 | 0/0 | blue 18/17 | Won ✔ |
| level_014 | OK | 3 / 7 | 5 | 48 | 0/0 | yellow 16/16 | Won ✔ |
| level_015 | OK | 4 / 10 | 9 | 73 | 0/0 | coral 33/33 | Won ✔ |
| level_016 | OK | 4 / 8 | 8 | 66 | 0/0 | blue 24/24, yellow 31/28 | Won ✔ |
| level_017 | OK | 6 / 12 | 11 | 91 | 0/0 | yellow 24/24, coral 38/38 | Won ✔ |
| level_018 | OK | 5 / 10 | 11 | 90 | 0/0 | coral 41/41, blue 32/32 | Won ✔ |
| level_019 | OK | 4 / 10 | 10 | 78 | 0/0 | blue 28/24, yellow 31/26 | Won ✔ |
| level_020 | OK | 5 / 11 | 12 | 91 | 0/0 | yellow 28/28, coral 32/31 | Won ✔ |
| level_021 | OK | 6 / 11 | 12 | 69 | 0/0 | purple 20/20, yellow 20/16 | Won ✔ |
| level_022 | OK | 5 / 10 | 12 | 86 | 0/0 | yellow 30/29, blue 19/18 | Won ✔ |
| level_023 | OK | 4 / 8 | 9 | 67 | 0/0 | blue 26/25, coral 19/19 | Won ✔ |
| level_024 | OK | 3 / 9 | 11 | 66 | 0/0 | coral 25/23, purple 12/12 | Won ✔ |
| level_025 | OK | 5 / 8 | 11 | 68 | 0/0 | purple 21/21, yellow 25/23 | Won ✔ |
| level_026 | OK | 3 / 7 | 9 | 61 | 0/0 | yellow 23/23, blue 21/20 | Won ✔ |
| level_027 | OK | 5 / 8 | 16 | 78 | 0/0 | blue 15/15, coral 21/21 | Won ✔ |
| level_028 | OK | 4 / 6 | 11 | 67 | 0/0 | coral 27/25, purple 26/26 | Won ✔ |
| level_029 | OK | 6 / 9 | 13 | 77 | 0/0 | purple 26/25, yellow 25/23 | Won ✔ |
| level_030 | OK | 4 / 5 | 10 | 60 | 0/0 | yellow 29/29, blue 23/19 | Won ✔ |
| level_031 | OK | 5 / 9 | 13 | 93 | 0/0 | blue 36/36, coral 14/14 | Won ✔ |
| level_032 | OK | 6 / 11 | 12 | 98 | 0/0 | coral 32/31, purple 9/9 | Won ✔ |
| level_033 | OK | 4 / 7 | 12 | 97 | 0/0 | purple 29/27, yellow 47/42 | Won ✔ |
| level_034 | OK | 3 / 7 | 10 | 86 | 0/0 | yellow 37/35, blue 23/23 | Won ✔ |
| level_035 | OK | 6 / 11 | 17 | 124 | 0/0 | blue 34/34, coral 50/49 | Won ✔ |
| level_036 | OK | 5 / 8 | 15 | 111 | 0/0 | coral 41/41, purple 21/20 | Won ✔ |
| level_037 | OK | 8 / 10 | 17 | 89 | 0/0 | purple 34/34, yellow 35/34 | Won ✔ |
| level_038 | OK | 5 / 6 | 15 | 120 | 0/0 | yellow 30/30, blue 24/24 | Won ✔ |
| level_039 | OK | 5 / 12 | 14 | 101 | 0/0 | blue 26/26, coral 41/40 | Won ✔ |
| level_040 | OK | 5 / 9 | 12 | 83 | 0/0 | coral 26/26, purple 18/18 | Won ✔ |
| level_041 | OK | 3 / 9 | 7 | 59 | 0/0 | purple 25/25, yellow 9/9, star 2/2 | Won ✔ |
| level_042 | OK | 6 / 10 | 13 | 67 | 0/0 | yellow 24/22, blue 14/14, star 3/3 | Won ✔ |
| level_043 | OK | 4 / 8 | 12 | 92 | 0/0 | blue 10/10, coral 33/22, star 3/3 | Won ✔ |
| level_044 | OK | 6 / 11 | 18 | 117 | 0/0 | coral 29/29, purple 41/41, star 3/3 | Won ✔ |
| level_045 | OK | 4 / 8 | 12 | 106 | 0/0 | purple 34/34, yellow 22/20, star 2/2 | Won ✔ |
| level_046 | OK | 7 / 12 | 17 | 130 | 0/0 | yellow 37/36, blue 34/28, star 3/3 | Won ✔ |
| level_047 | OK | 5 / 10 | 15 | 84 | 0/0 | blue 32/32, star 3/3, envelope 6/6 | Won ✔ |
| level_048 | OK | 5 / 8 | 16 | 87 | 0/0 | coral 19/18, purple 16/15, star 3/3 | Won ✔ |
| level_049 | OK | 1 / 1 | 3 | 11 | 0/0 | yellow 8/8 | Won ✔ |
| level_050 | OK | 4 / 5 | 10 | 93 | 0/0 | yellow 24/24, star 5/5, envelope 10/6 | Won ✔ |
| level_051 | OK | 5 / 11 | 12 | 72 | 0/0 | blue 38/37, coral 18/18 | Won ✔ |
| level_052 | OK | 5 / 10 | 15 | 108 | 0/0 | coral 29/28, purple 30/29 | Won ✔ |
| level_053 | OK | 9 / 13 | 18 | 116 | 0/0 | purple 23/21, yellow 31/28, star 2/2 | Won ✔ |
| level_054 | OK | 8 / 12 | 17 | 112 | 0/0 | yellow 46/45, blue 27/26 | Won ✔ |
| level_055 | OK | 4 / 8 | 13 | 108 | 0/0 | blue 23/23, coral 20/20, star 2/2 | Won ✔ |
| level_056 | OK | 3 / 7 | 12 | 127 | 0/0 | coral 51/45, purple 35/34, star 3/3 | Won ✔ |
| level_057 | OK | 6 / 11 | 16 | 95 | 0/0 | purple 34/34, yellow 15/14 | Won ✔ |
| level_058 | OK | 4 / 9 | 12 | 103 | 0/0 | yellow 46/46, blue 29/29 | Won ✔ |
| level_059 | OK | 5 / 10 | 13 | 101 | 0/0 | blue 26/26, coral 18/16, star 3/3 | Won ✔ |
| level_060 | OK | 7 / 10 | 19 | 107 | 0/0 | coral 37/32, purple 24/18, star 3/3 | Won ✔ |
| level_061 | OK | 6 / 10 | 16 | 134 | 0/0 | purple 33/33, yellow 39/39 | Won ✔ |
| level_062 | OK | 5 / 9 | 12 | 95 | 0/0 | yellow 25/24, blue 45/45 | Won ✔ |
| level_063 | OK | 5 / 11 | 14 | 103 | 0/0 | blue 28/27, coral 35/34 | Won ✔ |
| level_064 | OK | 7 / 13 | 20 | 125 | 0/0 | coral 38/38, purple 31/31 | Won ✔ |
| level_065 | OK | 5 / 9 | 14 | 93 | 0/0 | purple 47/47, yellow 19/19 | Won ✔ |
| level_066 | OK | 6 / 10 | 15 | 115 | 0/0 | yellow 30/29, blue 27/25 | Won ✔ |
| level_067 | OK | 4 / 9 | 11 | 92 | 0/0 | blue 25/23, coral 17/16 | Won ✔ |
| level_068 | OK | 4 / 7 | 9 | 68 | 0/0 | coral 23/23, purple 31/31 | Won ✔ |
| level_069 | OK | 4 / 7 | 10 | 69 | 0/0 | purple 26/26, yellow 11/10 | Won ✔ |
| level_070 | OK | 5 / 7 | 10 | 92 | 0/0 | blue 25/25, envelope 15/6 | Won ✔ |
| level_071 | OK | 6 / 11 | 17 | 121 | 0/0 | blue 29/29, coral 47/47, star 2/2 | Won ✔ |
| level_072 | OK | 6 / 13 | 15 | 124 | 0/0 | coral 25/25, purple 33/33, star 3/3 | Won ✔ |
| level_073 | OK | 6 / 12 | 14 | 106 | 0/0 | purple 29/29, yellow 21/21 | Won ✔ |
| level_074 | OK | 8 / 12 | 16 | 107 | 0/0 | yellow 26/26, blue 18/18 | Won ✔ |
| level_075 | OK | 6 / 14 | 12 | 87 | 0/0 | blue 36/36, coral 25/25 | Won ✔ |
| level_076 | OK | 8 / 14 | 15 | 113 | 0/0 | coral 35/35, purple 20/20 | Won ✔ |
| level_077 | OK | 13 / 16 | 17 | 91 | 0/0 | purple 35/33, yellow 25/15 | Won ✔ |
| level_078 | OK | 7 / 12 | 12 | 72 | 0/0 | yellow 31/31, blue 23/23 | Won ✔ |
| level_079 | OK | 5 / 13 | 12 | 92 | 0/0 | blue 31/31, coral 29/27, star 2/2 | Won ✔ |
| level_080 | OK | 5 / 8 | 13 | 91 | 0/0 | coral 38/37, purple 16/16 | Won ✔ |
| level_081 | OK | 5 / 14 | 12 | 93 | 0/0 | purple 20/17, yellow 37/30, star 2/2 | Won ✔ |
| level_082 | OK | 6 / 13 | 16 | 120 | 0/0 | yellow 33/33, blue 26/20, star 2/2 | Won ✔ |
| level_083 | OK | 5 / 9 | 13 | 105 | 0/0 | blue 28/27, coral 33/23, star 5/5 | Won ✔ |
| level_084 | OK | 3 / 6 | 12 | 72 | 0/0 | coral 25/25, purple 23/23, star 3/3 | Won ✔ |
| level_085 | OK | 4 / 10 | 13 | 111 | 0/0 | purple 36/35, yellow 24/23, star 3/3 | Won ✔ |
| level_086 | OK | 4 / 8 | 13 | 104 | 0/0 | yellow 21/19, blue 19/14, star 3/3 | Won ✔ |
| level_087 | OK | 4 / 8 | 12 | 89 | 0/0 | blue 38/37, coral 28/27, star 3/3 | Won ✔ |
| level_088 | OK | 5 / 8 | 10 | 72 | 0/0 | coral 35/33, purple 17/15, star 3/3 | Won ✔ |
| level_089 | OK | 5 / 9 | 14 | 103 | 0/0 | purple 21/20, yellow 27/27, star 5/5 | Won ✔ |
| level_090 | OK | 7 / 19 | 18 | 122 | 0/0 | blue 20/16, star 5/5, envelope 6/6 | Won ✔ |
| level_091 | OK | 6 / 15 | 14 | 114 | 0/0 | blue 51/51, coral 16/16 | Won ✔ |
| level_092 | OK | 4 / 8 | 11 | 102 | 0/0 | coral 20/20, purple 21/21 | Won ✔ |
| level_093 | OK | 5 / 8 | 10 | 64 | 0/0 | purple 24/24, yellow 25/23 | Won ✔ |
| level_094 | OK | 6 / 9 | 13 | 107 | 0/0 | yellow 44/44, blue 22/21 | Won ✔ |
| level_095 | OK | 6 / 9 | 13 | 102 | 0/0 | blue 46/43, coral 33/32 | Won ✔ |
| level_096 | OK | 4 / 8 | 9 | 64 | 0/0 | coral 23/23, purple 18/17 | Won ✔ |
| level_097 | OK | 5 / 8 | 16 | 100 | 0/0 | purple 28/27, yellow 19/18 | Won ✔ |
| level_098 | OK | 8 / 12 | 13 | 74 | 0/0 | yellow 26/26, blue 16/13 | Won ✔ |
| level_099 | OK | 1 / 1 | 3 | 18 | 0/0 | coral 9/9 | Won ✔ |
| level_100 | OK | 7 / 8 | 22 | 181 | 0/0 | coral 51/50, purple 44/44, star 2/2 | Won ✔ |
| level_1000 | OK | 7 / 11 | 23 | 236 | 0/0 | coral 41/39, purple 65/64, yellow 73/70, sponge 1/1 | Won ✔ |
| level_101 | OK | 4 / 9 | 11 | 89 | 0/0 | purple 30/30, yellow 29/27 | Won ✔ |
| level_102 | OK | 5 / 10 | 13 | 118 | 0/0 | yellow 16/16, blue 32/31 | Won ✔ |
| level_103 | OK | 5 / 9 | 16 | 118 | 0/0 | blue 32/32, coral 36/36 | Won ✔ |
| level_104 | OK | 5 / 8 | 13 | 91 | 0/0 | coral 25/24, purple 22/21 | Won ✔ |
| level_105 | OK | 4 / 8 | 11 | 89 | 0/0 | purple 28/28, yellow 34/33 | Won ✔ |
| level_106 | OK | 4 / 8 | 11 | 85 | 0/0 | yellow 29/29, blue 23/22 | Won ✔ |
| level_107 | OK | 7 / 9 | 15 | 117 | 0/0 | blue 33/32, coral 30/30 | Won ✔ |
| level_108 | OK | 7 / 9 | 24 | 140 | 0/0 | coral 63/63, purple 24/23 | Won ✔ |
| level_109 | OK | 10 / 14 | 20 | 138 | 0/0 | purple 50/50, yellow 38/38 | Won ✔ |
| level_110 | OK | 9 / 12 | 24 | 169 | 0/0 | yellow 36/36, blue 39/35, star 3/3 | Won ✔ |
| level_111 | OK | 5 / 10 | 16 | 104 | 0/0 | blue 47/45, coral 26/25, star 2/2 | Won ✔ |
| level_112 | OK | 4 / 8 | 12 | 98 | 0/0 | coral 19/19, purple 26/22 | Won ✔ |
| level_113 | OK | 5 / 9 | 15 | 116 | 0/0 | purple 48/48, yellow 25/24, star 2/2 | Won ✔ |
| level_114 | OK | 8 / 12 | 15 | 72 | 0/0 | yellow 21/21, blue 22/21 | Won ✔ |
| level_115 | OK | 9 / 15 | 22 | 116 | 0/0 | blue 36/36, coral 33/33 | Won ✔ |
| level_116 | OK | 6 / 12 | 16 | 117 | 0/0 | coral 44/44, purple 18/17, star 2/2 | Won ✔ |
| level_117 | OK | 8 / 11 | 16 | 135 | 0/0 | purple 42/42, yellow 41/40 | Won ✔ |
| level_118 | OK | 4 / 7 | 12 | 116 | 0/0 | yellow 43/43, blue 29/27 | Won ✔ |
| level_119 | OK | 8 / 12 | 20 | 149 | 0/0 | blue 49/49, coral 48/47, star 3/3 | Won ✔ |
| level_120 | OK | 10 / 16 | 27 | 221 | 0/0 | coral 76/75, purple 45/45, star 3/3 | Won ✔ |
| level_121 | OK | 3 / 7 | 12 | 98 | 0/0 | purple 30/30, yellow 30/29 | Won ✔ |
| level_122 | OK | 4 / 8 | 12 | 91 | 0/0 | yellow 21/21, blue 27/27 | Won ✔ |
| level_123 | OK | 5 / 8 | 13 | 100 | 0/0 | blue 27/27, coral 27/24 | Won ✔ |
| level_124 | OK | 6 / 9 | 16 | 116 | 0/0 | coral 28/28, purple 39/38 | Won ✔ |
| level_125 | OK | 5 / 9 | 14 | 106 | 0/0 | purple 48/47, yellow 32/32 | Won ✔ |
| level_126 | OK | 6 / 11 | 18 | 133 | 0/0 | yellow 44/44, blue 32/30 | Won ✔ |
| level_127 | OK | 5 / 7 | 13 | 115 | 0/0 | blue 27/25, coral 23/23 | Won ✔ |
| level_128 | OK | 6 / 8 | 16 | 126 | 0/0 | coral 35/35, purple 34/33 | Won ✔ |
| level_129 | OK | 4 / 7 | 13 | 106 | 0/0 | purple 30/29, yellow 45/43 | Won ✔ |
| level_130 | OK | 4 / 9 | 11 | 132 | 0/0 | blue 57/57, envelope 9/6 | Won ✔ |
| level_131 | OK | 6 / 11 | 15 | 111 | 0/0 | blue 37/37, coral 33/33 | Won ✔ |
| level_132 | OK | 5 / 10 | 15 | 114 | 0/0 | coral 45/45, purple 39/39 | Won ✔ |
| level_133 | OK | 5 / 9 | 11 | 77 | 0/0 | purple 33/33, yellow 25/24 | Won ✔ |
| level_134 | OK | 7 / 10 | 17 | 102 | 0/0 | yellow 32/32, blue 32/30 | Won ✔ |
| level_135 | OK | 5 / 9 | 12 | 105 | 0/0 | blue 39/39, coral 35/31 | Won ✔ |
| level_136 | OK | 4 / 8 | 13 | 89 | 0/0 | coral 41/41, purple 31/28 | Won ✔ |
| level_137 | OK | 12 / 13 | 22 | 176 | 0/0 | purple 59/58, yellow 47/41 | Won ✔ |
| level_138 | OK | 6 / 9 | 14 | 106 | 0/0 | yellow 39/37, blue 26/26 | Won ✔ |
| level_139 | OK | 4 / 9 | 12 | 118 | 0/0 | blue 25/23, coral 48/45 | Won ✔ |
| level_140 | OK | 5 / 6 | 11 | 80 | 0/0 | coral 30/29, purple 23/22, star 2/2 | Won ✔ |
| level_141 | OK | 7 / 11 | 18 | 111 | 0/0 | purple 48/48, yellow 25/24 | Won ✔ |
| level_142 | OK | 7 / 14 | 18 | 143 | 0/0 | yellow 37/36, blue 54/54 | Won ✔ |
| level_143 | OK | 9 / 15 | 22 | 134 | 0/0 | blue 35/35, coral 31/31 | Won ✔ |
| level_144 | OK | 6 / 12 | 17 | 157 | 0/0 | coral 40/37, purple 41/39 | Won ✔ |
| level_145 | OK | 5 / 8 | 12 | 101 | 0/0 | purple 36/35, yellow 26/24 | Won ✔ |
| level_146 | OK | 6 / 10 | 16 | 138 | 0/0 | yellow 64/63, blue 28/28 | Won ✔ |
| level_147 | OK | 8 / 12 | 15 | 117 | 0/0 | blue 41/37, coral 30/29 | Won ✔ |
| level_148 | OK | 16 / 19 | 23 | 109 | 0/0 | coral 28/28, purple 20/18 | Won ✔ |
| level_149 | OK | 1 / 1 | 3 | 15 | 0/0 | yellow 11/11 | Won ✔ |
| level_150 | OK | 10 / 15 | 23 | 140 | 0/0 | yellow 23/22, blue 33/33, star 2/2 | Won ✔ |
| level_151 | OK | 3 / 7 | 13 | 138 | 0/0 | blue 44/44, coral 37/37 | Won ✔ |
| level_152 | OK | 5 / 10 | 13 | 86 | 0/0 | coral 33/33, purple 40/40 | Won ✔ |
| level_153 | OK | 5 / 13 | 16 | 138 | 0/0 | purple 41/41, yellow 38/36 | Won ✔ |
| level_154 | OK | 6 / 11 | 15 | 89 | 0/0 | yellow 38/38, blue 23/23 | Won ✔ |
| level_155 | OK | 9 / 15 | 23 | 134 | 0/0 | blue 39/39, coral 29/28 | Won ✔ |
| level_156 | OK | 5 / 9 | 15 | 100 | 0/0 | coral 37/37, purple 27/27 | Won ✔ |
| level_157 | OK | 7 / 9 | 18 | 125 | 0/0 | purple 41/41, yellow 22/22 | Won ✔ |
| level_158 | OK | 6 / 11 | 14 | 137 | 0/0 | yellow 47/47, blue 34/34 | Won ✔ |
| level_159 | OK | 5 / 13 | 11 | 91 | 0/0 | blue 36/35, coral 24/21 | Won ✔ |
| level_160 | OK | 6 / 8 | 18 | 172 | 0/0 | coral 43/39, purple 49/48, yellow 34/29, star 2/2 | Won ✔ |
| level_161 | OK | 5 / 11 | 15 | 108 | 0/0 | purple 49/36, yellow 19/17, star 2/2 | Won ✔ |
| level_162 | OK | 6 / 10 | 14 | 107 | 0/0 | yellow 28/28, blue 24/24, star 3/3 | Won ✔ |
| level_163 | OK | 5 / 9 | 18 | 150 | 0/0 | blue 50/50, coral 22/21, star 3/3 | Won ✔ |
| level_164 | OK | 6 / 13 | 16 | 137 | 0/0 | coral 45/45, purple 39/38, star 3/3 | Won ✔ |
| level_165 | OK | 4 / 8 | 11 | 86 | 0/0 | purple 29/29, yellow 28/25, star 3/3 | Won ✔ |
| level_166 | OK | 12 / 17 | 21 | 161 | 0/0 | yellow 28/18, blue 51/36, star 3/3 | Won ✔ |
| level_167 | OK | 8 / 8 | 21 | 156 | 0/0 | blue 32/32, coral 52/52, star 3/3 | Won ✔ |
| level_168 | OK | 12 / 12 | 25 | 153 | 0/0 | coral 18/15, purple 50/38, star 2/2 | Won ✔ |
| level_169 | OK | 5 / 11 | 17 | 147 | 0/0 | purple 60/47, yellow 24/24, blue 33/22, star 5/5 | Won ✔ |
| level_170 | OK | 10 / 12 | 29 | 198 | 0/0 | yellow 44/43, blue 37/36, coral 65/64, star 5/5 | Won ✔ |
| level_171 | OK | 7 / 14 | 19 | 113 | 0/0 | blue 33/33, coral 43/43 | Won ✔ |
| level_172 | OK | 5 / 10 | 18 | 106 | 0/0 | coral 32/31, purple 26/25 | Won ✔ |
| level_173 | OK | 5 / 13 | 15 | 122 | 0/0 | purple 28/27, yellow 39/38 | Won ✔ |
| level_174 | OK | 7 / 13 | 17 | 117 | 0/0 | yellow 39/39, blue 21/20 | Won ✔ |
| level_175 | OK | 8 / 11 | 20 | 151 | 0/0 | blue 37/37, coral 48/48 | Won ✔ |
| level_176 | OK | 5 / 10 | 13 | 135 | 0/0 | coral 30/30, purple 43/40 | Won ✔ |
| level_177 | OK | 7 / 9 | 15 | 131 | 0/0 | purple 42/42, blue 45/40, envelope 6/3 | Won ✔ |
| level_178 | OK | 4 / 6 | 14 | 98 | 0/0 | yellow 31/31, blue 38/37, coral 18/17 | Won ✔ |
| level_179 | OK | 5 / 8 | 13 | 109 | 0/0 | blue 35/33, coral 37/37, purple 20/18 | Won ✔ |
| level_180 | OK | 6 / 9 | 16 | 128 | 0/0 | coral 44/41, purple 24/24, yellow 40/35, star 3/3 | Won ✔ |
| level_181 | OK | 10 / 17 | 23 | 140 | 0/0 | purple 46/46, yellow 50/49 | Won ✔ |
| level_182 | OK | 4 / 9 | 13 | 107 | 0/0 | yellow 29/28, blue 33/30 | Won ✔ |
| level_183 | OK | 5 / 9 | 13 | 92 | 0/0 | blue 30/30, coral 40/38 | Won ✔ |
| level_184 | OK | 8 / 12 | 19 | 142 | 0/0 | coral 36/35, purple 51/51 | Won ✔ |
| level_185 | OK | 7 / 10 | 16 | 143 | 0/0 | purple 51/51, yellow 36/32 | Won ✔ |
| level_186 | OK | 7 / 15 | 17 | 133 | 0/0 | yellow 24/24, blue 30/30 | Won ✔ |
| level_187 | OK | 9 / 11 | 17 | 138 | 0/0 | blue 36/36, coral 32/32, purple 41/33 | Won ✔ |
| level_188 | OK | 15 / 21 | 20 | 132 | 0/0 | coral 31/28, purple 35/32, yellow 32/32 | Won ✔ |
| level_189 | OK | 6 / 12 | 15 | 146 | 0/0 | purple 35/35, yellow 43/43, blue 37/37 | Won ✔ |
| level_190 | OK | 9 / 13 | 24 | 190 | 0/0 | yellow 46/45, blue 57/48, coral 31/31, star 2/2 | Won ✔ |
| level_191 | OK | 7 / 11 | 15 | 133 | 0/0 | blue 59/59, coral 27/24 | Won ✔ |
| level_192 | OK | 6 / 11 | 16 | 143 | 0/0 | coral 20/18, purple 48/47 | Won ✔ |
| level_193 | OK | 7 / 13 | 20 | 151 | 0/0 | purple 42/42, yellow 42/36, star 2/2 | Won ✔ |
| level_194 | OK | 11 / 16 | 18 | 122 | 0/0 | yellow 47/47, blue 41/41 | Won ✔ |
| level_195 | OK | 6 / 9 | 15 | 128 | 0/0 | blue 33/33, coral 28/26 | Won ✔ |
| level_196 | OK | 4 / 10 | 14 | 110 | 0/0 | coral 35/26, purple 28/22, star 3/3 | Won ✔ |
| level_197 | OK | 6 / 11 | 16 | 166 | 0/0 | purple 46/45, yellow 32/31, blue 33/33 | Won ✔ |
| level_198 | OK | 7 / 10 | 21 | 170 | 0/0 | yellow 60/57, blue 42/42, coral 23/23 | Won ✔ |
| level_199 | OK | 1 / 1 | 3 | 21 | 0/0 | coral 16/16 | Won ✔ |
| level_200 | OK | 8 / 15 | 22 | 164 | 0/0 | coral 33/33, purple 45/45, yellow 47/46, star 3/3 | Won ✔ |
| level_201 | OK | 5 / 9 | 15 | 122 | 0/0 | purple 22/22, yellow 33/33, envelope 1/1 | Won ✔ |
| level_202 | OK | 5 / 10 | 14 | 125 | 0/0 | yellow 40/40, blue 32/30, envelope 1/1 | Won ✔ |
| level_203 | OK | 6 / 9 | 14 | 115 | 0/0 | blue 27/27, coral 40/40, envelope 1/1 | Won ✔ |
| level_204 | OK | 5 / 11 | 15 | 84 | 0/0 | coral 29/29, purple 26/24, envelope 1/1 | Won ✔ |
| level_205 | OK | 7 / 11 | 18 | 113 | 0/0 | purple 40/39, yellow 30/30, envelope 1/1 | Won ✔ |
| level_206 | OK | 10 / 13 | 23 | 179 | 0/0 | yellow 44/42, blue 36/36, envelope 1/1 | Won ✔ |
| level_207 | OK | 6 / 9 | 16 | 137 | 0/0 | blue 44/44, coral 22/22, purple 34/33, envelope 1/1 | Won ✔ |
| level_208 | OK | 5 / 7 | 13 | 152 | 0/0 | coral 49/46, purple 41/40, yellow 24/24, envelope 1/1 | Won ✔ |
| level_209 | OK | 6 / 10 | 19 | 138 | 0/0 | purple 42/42, yellow 35/33, blue 34/31, envelope 1/1 | Won ✔ |
| level_210 | OK | 6 / 12 | 18 | 165 | 0/0 | yellow 51/51, blue 45/44, coral 29/29, envelope 1/1 | Won ✔ |
| level_211 | OK | 4 / 8 | 10 | 96 | 0/0 | blue 43/43, coral 36/36, envelope 2/2 | Won ✔ |
| level_212 | OK | 5 / 10 | 15 | 125 | 0/0 | coral 36/36, purple 34/34, envelope 2/2 | Won ✔ |
| level_213 | OK | 6 / 9 | 16 | 115 | 0/0 | purple 38/38, yellow 33/33, envelope 2/2 | Won ✔ |
| level_214 | OK | 7 / 11 | 19 | 156 | 0/0 | yellow 45/45, blue 35/34, envelope 2/2 | Won ✔ |
| level_215 | OK | 8 / 12 | 19 | 151 | 0/0 | blue 39/39, coral 27/26, envelope 2/2 | Won ✔ |
| level_216 | OK | 5 / 13 | 13 | 110 | 0/0 | coral 41/40, purple 29/29, envelope 2/2 | Won ✔ |
| level_217 | OK | 6 / 10 | 17 | 117 | 0/0 | yellow 32/32, blue 33/32, envelope 2/2, envelope 14/3 | Won ✔ |
| level_218 | OK | 5 / 8 | 13 | 81 | 0/0 | yellow 37/35, blue 22/19, coral 15/13, envelope 2/2 | Won ✔ |
| level_219 | OK | 6 / 10 | 15 | 124 | 0/0 | blue 36/36, coral 29/29, purple 36/32, star 2/2, envelope 2/2 | Won ✔ |
| level_220 | OK | 5 / 9 | 19 | 177 | 0/0 | coral 48/48, purple 46/45, yellow 45/41, envelope 2/2 | Won ✔ |
| level_221 | OK | 5 / 10 | 16 | 110 | 0/0 | purple 40/39, yellow 38/38, envelope 2/2 | Won ✔ |
| level_222 | OK | 8 / 13 | 17 | 96 | 0/0 | yellow 30/30, blue 22/22, envelope 2/2 | Won ✔ |
| level_223 | OK | 6 / 12 | 13 | 105 | 0/0 | blue 60/59, coral 24/24, envelope 2/2 | Won ✔ |
| level_224 | OK | 8 / 14 | 20 | 152 | 0/0 | coral 41/41, purple 43/42, envelope 2/2 | Won ✔ |
| level_225 | OK | 10 / 18 | 25 | 142 | 0/0 | purple 47/47, yellow 44/44, envelope 2/2 | Won ✔ |
| level_226 | OK | 7 / 12 | 16 | 137 | 0/0 | yellow 36/36, blue 31/31, envelope 2/2 | Won ✔ |
| level_227 | OK | 8 / 10 | 17 | 145 | 0/0 | blue 41/41, coral 34/34, purple 40/38, envelope 2/2 | Won ✔ |
| level_228 | OK | 5 / 9 | 15 | 111 | 0/0 | coral 28/27, purple 28/24, yellow 27/21, envelope 2/2 | Won ✔ |
| level_229 | OK | 8 / 14 | 24 | 165 | 0/0 | purple 34/34, yellow 37/37, blue 58/58, envelope 3/3 | Won ✔ |
| level_230 | OK | 7 / 10 | 21 | 162 | 0/0 | yellow 45/44, blue 49/49, coral 36/36, star 2/2, envelope 3/3 | Won ✔ |
| level_231 | OK | 7 / 11 | 17 | 103 | 0/0 | blue 36/35, coral 29/28, envelope 1/1 | Won ✔ |
| level_232 | OK | 8 / 15 | 21 | 141 | 0/0 | coral 36/36, purple 34/34, envelope 1/1 | Won ✔ |
| level_233 | OK | 7 / 13 | 14 | 115 | 0/0 | purple 51/48, yellow 20/19, envelope 2/2 | Won ✔ |
| level_234 | OK | 7 / 12 | 17 | 127 | 0/0 | yellow 37/37, blue 26/24, envelope 1/1 | Won ✔ |
| level_235 | OK | 7 / 13 | 15 | 157 | 0/0 | blue 42/42, coral 48/46, envelope 2/2 | Won ✔ |
| level_236 | OK | 7 / 10 | 17 | 128 | 0/0 | coral 45/41, purple 33/33, star 3/3, envelope 1/1 | Won ✔ |
| level_237 | OK | 5 / 8 | 15 | 139 | 0/0 | purple 53/47, yellow 27/27, blue 24/24, envelope 2/2 | Won ✔ |
| level_238 | OK | 9 / 12 | 20 | 171 | 0/0 | yellow 49/49, blue 40/40, coral 51/41, envelope 1/1 | Won ✔ |
| level_239 | OK | 8 / 15 | 19 | 123 | 0/0 | blue 38/38, coral 22/22, purple 31/31, star 2/2, envelope 2/2 | Won ✔ |
| level_240 | OK | 11 / 13 | 31 | 170 | 0/0 | coral 40/40, purple 43/43, yellow 49/42, star 2/2, envelope 2/2 | Won ✔ |
| level_241 | OK | 5 / 11 | 17 | 178 | 0/0 | purple 51/49, yellow 53/49, star 2/2, envelope 1/1 | Won ✔ |
| level_242 | OK | 5 / 12 | 16 | 153 | 0/0 | yellow 25/25, blue 40/38, star 3/3, envelope 1/1 | Won ✔ |
| level_243 | OK | 7 / 11 | 16 | 131 | 0/0 | blue 27/25, coral 57/46, star 2/2, envelope 2/2 | Won ✔ |
| level_244 | OK | 6 / 10 | 19 | 168 | 0/0 | coral 35/35, purple 60/59, yellow 32/27, star 3/3, envelope 1/1 | Won ✔ |
| level_245 | OK | 8 / 11 | 20 | 188 | 0/0 | purple 53/52, yellow 49/49, blue 50/40, star 2/2, envelope 2/2 | Won ✔ |
| level_246 | OK | 5 / 10 | 13 | 127 | 0/0 | yellow 42/41, blue 27/25, coral 28/22, star 3/3, envelope 1/1 | Won ✔ |
| level_247 | OK | 8 / 11 | 20 | 158 | 0/0 | blue 26/26, coral 60/60, purple 39/38, star 2/2, envelope 2/2 | Won ✔ |
| level_248 | OK | 5 / 8 | 12 | 151 | 0/0 | coral 37/35, purple 37/37, yellow 35/33, envelope 1/1 | Won ✔ |
| level_249 | OK | 1 / 1 | 3 | 10 | 0/0 | blue 7/7 | Won ✔ |
| level_250 | OK | 8 / 12 | 20 | 175 | 0/0 | yellow 44/44, blue 52/51, coral 37/36, star 3/3, envelope 2/2 | Won ✔ |
| level_251 | OK | 6 / 10 | 15 | 121 | 0/0 | blue 32/32, coral 43/42, envelope 1/1 | Won ✔ |
| level_252 | OK | 9 / 16 | 25 | 159 | 0/0 | coral 38/38, purple 36/34, envelope 1/1 | Won ✔ |
| level_253 | OK | 14 / 17 | 23 | 163 | 0/0 | purple 43/43, yellow 44/44, envelope 2/2 | Won ✔ |
| level_254 | OK | 6 / 11 | 16 | 110 | 0/0 | yellow 42/40, blue 24/22, coral 34/32, envelope 1/1 | Won ✔ |
| level_255 | OK | 7 / 15 | 21 | 135 | 0/0 | blue 36/36, coral 52/51, purple 25/23, envelope 2/2 | Won ✔ |
| level_256 | OK | 8 / 14 | 15 | 123 | 0/0 | coral 30/30, purple 31/30, yellow 33/31, envelope 1/1 | Won ✔ |
| level_257 | OK | 7 / 9 | 18 | 139 | 0/0 | purple 25/25, yellow 43/38, blue 38/26, envelope 2/2 | Won ✔ |
| level_258 | OK | 6 / 10 | 18 | 113 | 0/0 | yellow 36/36, blue 27/22, coral 18/17, envelope 1/1 | Won ✔ |
| level_259 | OK | 9 / 16 | 27 | 173 | 0/0 | blue 38/38, coral 43/42, purple 41/41, envelope 3/3 | Won ✔ |
| level_260 | OK | 7 / 10 | 25 | 184 | 0/0 | coral 48/47, purple 43/42, yellow 56/38, star 3/3, envelope 2/2 | Won ✔ |
| level_261 | OK | 7 / 12 | 16 | 114 | 0/0 | purple 42/42, yellow 35/35, envelope 1/1 | Won ✔ |
| level_262 | OK | 6 / 10 | 13 | 111 | 0/0 | yellow 41/40, blue 28/28, envelope 1/1 | Won ✔ |
| level_263 | OK | 7 / 10 | 17 | 130 | 0/0 | blue 33/32, coral 44/43, envelope 2/2 | Won ✔ |
| level_264 | OK | 6 / 12 | 14 | 103 | 0/0 | coral 25/25, purple 26/26, yellow 30/27, envelope 1/1 | Won ✔ |
| level_265 | OK | 7 / 10 | 16 | 116 | 0/0 | purple 32/32, yellow 38/38, blue 28/28, envelope 2/2 | Won ✔ |
| level_266 | OK | 5 / 8 | 13 | 127 | 0/0 | yellow 44/43, blue 44/44, coral 25/24, envelope 1/1 | Won ✔ |
| level_267 | OK | 6 / 8 | 18 | 117 | 0/0 | blue 24/22, coral 44/36, purple 20/20, envelope 2/2 | Won ✔ |
| level_268 | OK | 17 / 20 | 21 | 99 | 0/0 | coral 33/33, purple 25/23, yellow 15/15, envelope 1/1 | Won ✔ |
| level_269 | OK | 8 / 12 | 24 | 169 | 0/0 | purple 61/60, yellow 45/42, blue 32/32, envelope 3/3 | Won ✔ |
| level_270 | OK | 7 / 10 | 25 | 206 | 0/0 | yellow 82/82, blue 41/41, coral 36/35, star 2/2, envelope 2/2 | Won ✔ |
| level_271 | OK | 9 / 14 | 18 | 164 | 0/0 | blue 69/69, coral 37/36, envelope 1/1 | Won ✔ |
| level_272 | OK | 5 / 10 | 12 | 106 | 0/0 | coral 52/51, purple 31/31, envelope 1/1 | Won ✔ |
| level_273 | OK | 6 / 14 | 17 | 116 | 0/0 | purple 38/37, yellow 24/24, envelope 1/1 | Won ✔ |
| level_274 | OK | 7 / 15 | 18 | 151 | 0/0 | yellow 58/58, blue 46/46, coral 32/30, envelope 2/2 | Won ✔ |
| level_275 | OK | 7 / 10 | 17 | 152 | 0/0 | blue 24/24, coral 65/62, purple 45/45, envelope 1/1 | Won ✔ |
| level_276 | OK | 6 / 9 | 15 | 97 | 0/0 | coral 30/30, purple 39/38, yellow 19/19, envelope 2/2 | Won ✔ |
| level_277 | OK | 6 / 8 | 20 | 164 | 0/0 | purple 51/51, yellow 39/39, blue 33/28, envelope 2/2 | Won ✔ |
| level_278 | OK | 7 / 14 | 21 | 170 | 0/0 | yellow 25/25, blue 49/49, coral 50/49, envelope 1/1 | Won ✔ |
| level_279 | OK | 8 / 15 | 22 | 152 | 0/0 | blue 40/40, coral 37/31, purple 45/35, star 2/2, envelope 2/2 | Won ✔ |
| level_280 | OK | 8 / 11 | 26 | 210 | 0/0 | coral 55/55, purple 62/60, yellow 54/53, star 2/2, envelope 2/2 | Won ✔ |
| level_281 | OK | 11 / 18 | 26 | 179 | 0/0 | purple 41/41, yellow 42/42, envelope 1/1 | Won ✔ |
| level_282 | OK | 7 / 14 | 17 | 147 | 0/0 | yellow 58/56, blue 34/34, envelope 1/1 | Won ✔ |
| level_283 | OK | 7 / 15 | 20 | 157 | 0/0 | blue 48/47, coral 42/41, envelope 2/2 | Won ✔ |
| level_284 | OK | 6 / 11 | 16 | 148 | 0/0 | coral 44/44, purple 54/52, yellow 18/18, envelope 1/1 | Won ✔ |
| level_285 | OK | 6 / 11 | 15 | 136 | 0/0 | purple 52/51, yellow 32/32, blue 30/28, envelope 2/2 | Won ✔ |
| level_286 | OK | 8 / 12 | 18 | 174 | 0/0 | yellow 50/50, blue 62/62, coral 51/48, star 2/2, envelope 1/1 | Won ✔ |
| level_287 | OK | 8 / 9 | 21 | 173 | 0/0 | blue 42/42, coral 50/48, purple 42/42, envelope 2/2 | Won ✔ |
| level_288 | OK | 11 / 13 | 27 | 198 | 0/0 | coral 64/63, purple 37/36, yellow 37/36, envelope 1/1 | Won ✔ |
| level_289 | OK | 11 / 19 | 26 | 215 | 0/0 | purple 50/50, yellow 54/51, blue 73/64, envelope 3/3 | Won ✔ |
| level_290 | OK | 6 / 7 | 16 | 190 | 0/0 | yellow 38/37, blue 61/61, star 2/2, envelope 2/2, envelope 6/3 | Won ✔ |
| level_291 | OK | 6 / 11 | 19 | 136 | 0/0 | blue 37/37, coral 36/36, envelope 2/2 | Won ✔ |
| level_292 | OK | 10 / 15 | 21 | 154 | 0/0 | coral 54/54, purple 37/34, envelope 3/3 | Won ✔ |
| level_293 | OK | 7 / 13 | 18 | 153 | 0/0 | purple 50/50, yellow 43/43, envelope 2/2 | Won ✔ |
| level_294 | OK | 8 / 16 | 16 | 115 | 0/0 | yellow 37/36, blue 21/21, coral 28/21, envelope 2/2 | Won ✔ |
| level_295 | OK | 5 / 9 | 18 | 131 | 0/0 | blue 30/28, coral 41/41, purple 41/28, envelope 3/3 | Won ✔ |
| level_296 | OK | 5 / 8 | 12 | 101 | 1/0 | coral 30/28, purple 24/19, yellow 18/17, star 3/3, envelope 2/2 | Won ✔ |
| level_297 | OK | 7 / 9 | 16 | 133 | 0/0 | yellow 36/34, blue 35/34, envelope 2/2, envelope 10/6 | Won ✔ |
| level_298 | OK | 6 / 13 | 17 | 120 | 0/0 | yellow 31/31, blue 35/34, coral 43/29, envelope 1/1 | Won ✔ |
| level_299 | OK | 1 / 1 | 3 | 23 | 0/0 | coral 13/13 | Won ✔ |
| level_300 | OK | 8 / 10 | 20 | 173 | 0/0 | coral 46/46, purple 56/56, yellow 37/34, star 3/3, envelope 3/3 | Won ✔ |
| level_301 | OK | 7 / 11 | 17 | 126 | 0/0 | purple 43/43, yellow 32/31 | Won ✔ |
| level_302 | OK | 7 / 11 | 19 | 125 | 0/0 | yellow 42/42, blue 23/22 | Won ✔ |
| level_303 | OK | 4 / 8 | 9 | 78 | 0/0 | blue 30/27, coral 27/26 | Won ✔ |
| level_304 | OK | 6 / 9 | 14 | 130 | 0/0 | coral 19/19, purple 22/21 | Won ✔ |
| level_305 | OK | 10 / 15 | 21 | 149 | 0/0 | purple 36/36, yellow 44/40 | Won ✔ |
| level_306 | OK | 9 / 17 | 20 | 158 | 0/0 | yellow 34/34, blue 49/49, coral 56/55 | Won ✔ |
| level_307 | OK | 4 / 8 | 12 | 125 | 0/0 | blue 41/38, coral 30/26, purple 39/38 | Won ✔ |
| level_308 | OK | 10 / 14 | 13 | 84 | 0/0 | coral 16/16, purple 33/31, yellow 23/22 | Won ✔ |
| level_309 | OK | 6 / 14 | 12 | 98 | 0/0 | purple 25/25, yellow 27/25, blue 38/37 | Won ✔ |
| level_310 | OK | 7 / 9 | 19 | 193 | 0/0 | yellow 69/68, blue 58/54, coral 40/40 | Won ✔ |
| level_311 | OK | 8 / 12 | 15 | 111 | 0/0 | blue 44/44, coral 20/20 | Won ✔ |
| level_312 | OK | 11 / 14 | 22 | 102 | 0/0 | coral 37/36, purple 18/18 | Won ✔ |
| level_313 | OK | 4 / 8 | 12 | 123 | 0/0 | purple 28/27, yellow 33/32 | Won ✔ |
| level_314 | OK | 5 / 9 | 14 | 147 | 0/0 | yellow 37/37, blue 33/26, coral 45/34 | Won ✔ |
| level_315 | OK | 7 / 17 | 18 | 140 | 0/0 | blue 37/37, coral 39/38, purple 38/30 | Won ✔ |
| level_316 | OK | 4 / 10 | 11 | 105 | 0/0 | coral 24/24, purple 37/36, yellow 26/26 | Won ✔ |
| level_317 | OK | 7 / 10 | 21 | 169 | 0/0 | purple 44/44, yellow 63/62, blue 22/22 | Won ✔ |
| level_318 | OK | 6 / 7 | 16 | 125 | 0/0 | yellow 54/53, blue 19/19, coral 33/32 | Won ✔ |
| level_319 | OK | 6 / 16 | 15 | 159 | 0/0 | blue 69/68, coral 40/40, purple 46/41 | Won ✔ |
| level_320 | OK | 7 / 10 | 19 | 169 | 0/0 | coral 36/36, purple 40/38, yellow 52/50, star 2/2 | Won ✔ |
| level_321 | OK | 10 / 17 | 23 | 147 | 0/0 | purple 44/44, yellow 54/54 | Won ✔ |
| level_322 | OK | 7 / 17 | 21 | 174 | 0/0 | yellow 78/77, blue 27/27 | Won ✔ |
| level_323 | OK | 5 / 13 | 15 | 132 | 0/0 | blue 33/33, coral 30/30 | Won ✔ |
| level_324 | OK | 5 / 15 | 17 | 163 | 0/0 | coral 43/43, purple 43/43, yellow 50/47 | Won ✔ |
| level_325 | OK | 5 / 14 | 15 | 147 | 0/0 | purple 50/49, yellow 58/57, blue 39/32 | Won ✔ |
| level_326 | OK | 5 / 11 | 15 | 127 | 0/0 | yellow 37/36, blue 31/31, coral 39/39 | Won ✔ |
| level_327 | OK | 6 / 10 | 13 | 142 | 0/0 | blue 39/38, coral 31/31, purple 31/31 | Won ✔ |
| level_328 | OK | 6 / 8 | 21 | 189 | 0/0 | coral 56/56, purple 54/50, yellow 35/29 | Won ✔ |
| level_329 | OK | 6 / 8 | 16 | 132 | 0/0 | purple 41/41, yellow 35/33, blue 28/28 | Won ✔ |
| level_330 | OK | 5 / 10 | 20 | 193 | 0/0 | blue 66/65, coral 44/43, star 2/2, envelope 21/8 | Won ✔ |
| level_331 | OK | 8 / 15 | 20 | 167 | 0/0 | blue 44/43, coral 50/50 | Won ✔ |
| level_332 | OK | 8 / 15 | 19 | 152 | 0/0 | coral 26/26, purple 36/36, envelope 1/1 | Won ✔ |
| level_333 | OK | 9 / 17 | 19 | 164 | 0/0 | purple 62/62, yellow 35/35 | Won ✔ |
| level_334 | OK | 10 / 20 | 20 | 154 | 0/0 | yellow 51/51, blue 21/20, coral 47/41, envelope 2/2 | Won ✔ |
| level_335 | OK | 5 / 8 | 17 | 165 | 0/0 | blue 39/39, coral 38/37, purple 34/34 | Won ✔ |
| level_336 | OK | 8 / 18 | 14 | 107 | 0/0 | coral 28/27, purple 22/20, yellow 44/34, star 3/3 | Won ✔ |
| level_337 | OK | 9 / 16 | 28 | 175 | 0/0 | purple 46/46, yellow 38/38, envelope 9/4 | Won ✔ |
| level_338 | OK | 11 / 18 | 23 | 162 | 0/0 | yellow 39/38, blue 34/34, coral 52/44 | Won ✔ |
| level_339 | OK | 7 / 9 | 23 | 169 | 0/0 | blue 47/47, coral 33/33, purple 60/49, envelope 1/1 | Won ✔ |
| level_340 | OK | 19 / 25 | 30 | 161 | 0/0 | coral 58/55, purple 57/55, yellow 23/22, star 2/2, envelope 1/1 | Won ✔ |
| level_341 | OK | 5 / 14 | 14 | 135 | 0/0 | purple 33/33, yellow 44/42 | Won ✔ |
| level_342 | OK | 9 / 14 | 23 | 146 | 0/0 | yellow 47/47, blue 34/33 | Won ✔ |
| level_343 | OK | 8 / 12 | 19 | 156 | 0/0 | blue 65/65, coral 34/33 | Won ✔ |
| level_344 | OK | 5 / 11 | 17 | 155 | 0/0 | coral 52/52, purple 36/34, yellow 47/35 | Won ✔ |
| level_345 | OK | 5 / 8 | 16 | 124 | 0/0 | purple 24/24, yellow 32/31, blue 32/26 | Won ✔ |
| level_346 | OK | 5 / 9 | 16 | 146 | 0/0 | yellow 28/27, blue 23/22, coral 47/46 | Won ✔ |
| level_347 | OK | 9 / 16 | 21 | 187 | 0/0 | blue 50/47, coral 70/65, purple 44/40 | Won ✔ |
| level_348 | OK | 15 / 22 | 23 | 131 | 0/0 | coral 24/24, purple 31/25, yellow 28/17 | Won ✔ |
| level_349 | OK | 1 / 1 | 3 | 15 | 0/0 | blue 9/9 | Won ✔ |
| level_350 | OK | 6 / 9 | 19 | 153 | 0/0 | yellow 52/50, blue 39/39, coral 32/30, star 2/2 | Won ✔ |
| level_351 | OK | 5 / 10 | 15 | 114 | 0/0 | blue 38/38, coral 21/19, star 2/2 | Won ✔ |
| level_352 | OK | 7 / 11 | 19 | 137 | 0/0 | coral 44/40, purple 37/37, star 3/3 | Won ✔ |
| level_353 | OK | 5 / 7 | 15 | 117 | 0/0 | purple 32/32, yellow 39/38, star 2/2 | Won ✔ |
| level_354 | OK | 7 / 10 | 17 | 128 | 0/0 | yellow 32/32, blue 32/32, coral 33/30, star 3/3 | Won ✔ |
| level_355 | OK | 6 / 10 | 19 | 174 | 0/0 | blue 52/51, coral 56/54, purple 33/30, star 2/2 | Won ✔ |
| level_356 | OK | 7 / 15 | 17 | 150 | 0/0 | coral 34/31, purple 31/27, yellow 40/31, star 3/3 | Won ✔ |
| level_357 | OK | 9 / 14 | 22 | 197 | 0/0 | purple 47/47, yellow 57/57, blue 44/43, star 2/2 | Won ✔ |
| level_358 | OK | 8 / 12 | 23 | 164 | 0/0 | yellow 29/29, blue 54/52, coral 51/46 | Won ✔ |
| level_359 | OK | 6 / 16 | 19 | 128 | 0/0 | blue 47/47, coral 42/38, purple 23/21, star 3/3 | Won ✔ |
| level_360 | OK | 10 / 18 | 25 | 162 | 0/0 | coral 64/52, purple 24/24, yellow 41/29, star 3/3 | Won ✔ |
| level_361 | OK | 6 / 10 | 15 | 142 | 0/0 | purple 33/33, yellow 46/43 | Won ✔ |
| level_362 | OK | 8 / 15 | 21 | 149 | 0/0 | yellow 34/34, blue 33/33 | Won ✔ |
| level_363 | OK | 8 / 11 | 16 | 130 | 0/0 | blue 38/38, coral 30/28 | Won ✔ |
| level_364 | OK | 6 / 12 | 19 | 160 | 0/0 | coral 61/60, purple 34/32, yellow 36/32 | Won ✔ |
| level_365 | OK | 6 / 14 | 19 | 160 | 0/0 | purple 66/66, yellow 33/33, blue 21/21 | Won ✔ |
| level_366 | OK | 6 / 12 | 19 | 142 | 0/0 | yellow 44/44, blue 18/18, coral 36/34 | Won ✔ |
| level_367 | OK | 7 / 12 | 18 | 175 | 0/0 | blue 44/44, coral 38/38, purple 57/55 | Won ✔ |
| level_368 | OK | 9 / 12 | 21 | 181 | 0/0 | coral 53/53, purple 39/39, yellow 57/46 | Won ✔ |
| level_369 | OK | 7 / 14 | 20 | 161 | 0/0 | purple 49/49, yellow 44/44, blue 21/16 | Won ✔ |
| level_370 | OK | 6 / 13 | 20 | 176 | 0/0 | yellow 62/62, blue 51/49, star 2/2, envelope 11/10 | Won ✔ |
| level_371 | OK | 6 / 15 | 19 | 130 | 0/0 | blue 41/41, coral 29/29, envelope 1/1 | Won ✔ |
| level_372 | OK | 11 / 17 | 19 | 144 | 0/0 | coral 32/32, purple 39/37, envelope 1/1 | Won ✔ |
| level_373 | OK | 7 / 9 | 22 | 139 | 0/0 | purple 33/33, yellow 42/34, envelope 2/2 | Won ✔ |
| level_374 | OK | 9 / 17 | 22 | 181 | 0/0 | yellow 61/60, blue 58/54, coral 30/30, envelope 1/1 | Won ✔ |
| level_375 | OK | 5 / 8 | 14 | 124 | 0/0 | blue 22/22, coral 32/31, purple 44/29, envelope 2/2 | Won ✔ |
| level_376 | OK | 6 / 12 | 19 | 174 | 0/0 | coral 44/42, purple 48/47, yellow 45/44, envelope 1/1 | Won ✔ |
| level_377 | OK | 6 / 10 | 20 | 174 | 0/0 | yellow 43/42, blue 41/40, envelope 2/2, envelope 12/10 | Won ✔ |
| level_378 | OK | 7 / 15 | 20 | 128 | 0/0 | yellow 47/47, blue 33/33, coral 19/19, envelope 1/1 | Won ✔ |
| level_379 | OK | 8 / 14 | 25 | 190 | 0/0 | blue 38/38, coral 69/69, purple 36/36, envelope 3/3 | Won ✔ |
| level_380 | OK | 9 / 11 | 25 | 207 | 0/0 | coral 65/65, purple 51/51, yellow 51/51, star 2/2, envelope 2/2 | Won ✔ |
| level_381 | OK | 6 / 11 | 16 | 133 | 0/0 | purple 27/27, yellow 36/36 | Won ✔ |
| level_382 | OK | 7 / 11 | 16 | 153 | 0/0 | yellow 37/36, blue 38/26 | Won ✔ |
| level_383 | OK | 7 / 17 | 20 | 144 | 0/0 | blue 46/44, coral 35/34 | Won ✔ |
| level_384 | OK | 9 / 15 | 23 | 189 | 0/0 | coral 46/46, purple 63/63, yellow 57/55 | Won ✔ |
| level_385 | OK | 9 / 16 | 21 | 163 | 0/0 | purple 54/54, yellow 38/38, blue 33/32 | Won ✔ |
| level_386 | OK | 10 / 14 | 25 | 199 | 0/0 | yellow 57/57, blue 44/44, coral 47/47 | Won ✔ |
| level_387 | OK | 6 / 9 | 22 | 153 | 0/0 | blue 51/51, coral 34/34, purple 36/35 | Won ✔ |
| level_388 | OK | 14 / 19 | 18 | 130 | 0/0 | coral 40/39, purple 39/38, yellow 34/27 | Won ✔ |
| level_389 | OK | 7 / 15 | 23 | 198 | 0/0 | purple 47/47, yellow 58/57, blue 35/33 | Won ✔ |
| level_390 | OK | 10 / 17 | 28 | 178 | 0/0 | yellow 35/35, blue 62/51, coral 39/24, star 2/2 | Won ✔ |
| level_391 | OK | 6 / 13 | 16 | 173 | 0/0 | blue 25/25, coral 71/70 | Won ✔ |
| level_392 | OK | 10 / 19 | 23 | 175 | 0/0 | coral 50/50, purple 44/44 | Won ✔ |
| level_393 | OK | 5 / 9 | 13 | 143 | 0/0 | purple 40/33, yellow 51/48, envelope 1/1 | Won ✔ |
| level_394 | OK | 7 / 13 | 17 | 154 | 0/0 | yellow 40/39, blue 43/37, coral 41/31 | Won ✔ |
| level_395 | OK | 8 / 10 | 25 | 186 | 0/0 | blue 44/44, coral 52/51, purple 34/33 | Won ✔ |
| level_396 | OK | 6 / 12 | 19 | 124 | 0/0 | coral 35/35, purple 33/31, yellow 37/28, star 2/2, envelope 1/1 | Won ✔ |
| level_397 | OK | 8 / 11 | 17 | 166 | 0/0 | purple 61/61, yellow 54/54, blue 19/17 | Won ✔ |
| level_398 | OK | 5 / 10 | 20 | 174 | 0/0 | yellow 56/55, blue 35/35, coral 40/38 | Won ✔ |
| level_399 | OK | 1 / 1 | 3 | 22 | 0/0 | blue 16/16 | Won ✔ |
| level_400 | OK | 6 / 9 | 23 | 185 | 0/0 | coral 49/49, purple 39/39, yellow 56/44, star 3/3 | Won ✔ |
| level_401 | OK | 6 / 9 | 15 | 135 | 0/0 | purple 46/46, yellow 32/31, sponge 1/1 | Won ✔ |
| level_402 | OK | 8 / 12 | 19 | 127 | 0/0 | yellow 41/41, blue 24/24, sponge 1/1 | Won ✔ |
| level_403 | OK | 7 / 18 | 17 | 128 | 0/0 | blue 39/39, coral 39/37, sponge 1/1 | Won ✔ |
| level_404 | OK | 6 / 11 | 22 | 146 | 0/0 | coral 44/44, purple 26/25, yellow 31/31, sponge 1/1 | Won ✔ |
| level_405 | OK | 6 / 12 | 17 | 145 | 0/0 | purple 54/54, yellow 41/40, blue 33/28, sponge 1/1 | Won ✔ |
| level_406 | OK | 7 / 10 | 19 | 138 | 0/0 | yellow 42/42, blue 17/17, coral 42/39, envelope 2/2, sponge 1/1 | Won ✔ |
| level_407 | OK | 9 / 13 | 24 | 194 | 0/0 | blue 54/54, coral 43/43, purple 63/63, sponge 1/1 | Won ✔ |
| level_408 | OK | 6 / 7 | 16 | 121 | 0/0 | coral 36/36, purple 40/36, yellow 28/27, sponge 1/1 | Won ✔ |
| level_409 | OK | 8 / 12 | 26 | 207 | 0/0 | purple 69/69, yellow 40/40, blue 38/37, sponge 1/1 | Won ✔ |
| level_410 | OK | 12 / 16 | 32 | 183 | 0/0 | yellow 38/38, coral 52/47, sponge 1/1, envelope 10/10 | Won ✔ |
| level_411 | OK | 6 / 16 | 14 | 107 | 0/0 | blue 27/27, coral 36/35, sponge 1/1 | Won ✔ |
| level_412 | OK | 7 / 12 | 17 | 167 | 0/0 | coral 42/42, purple 35/35, sponge 1/1 | Won ✔ |
| level_413 | OK | 6 / 9 | 14 | 121 | 0/0 | purple 45/45, yellow 51/50, sponge 1/1 | Won ✔ |
| level_414 | OK | 7 / 15 | 16 | 146 | 0/0 | yellow 39/39, blue 59/59, coral 24/22, sponge 1/1 | Won ✔ |
| level_415 | OK | 6 / 10 | 16 | 138 | 0/0 | blue 58/58, coral 36/36, purple 32/23, sponge 1/1 | Won ✔ |
| level_416 | OK | 7 / 10 | 17 | 162 | 0/0 | coral 48/48, purple 41/41, yellow 35/31, sponge 1/1 | Won ✔ |
| level_417 | OK | 5 / 10 | 14 | 144 | 0/0 | purple 61/61, yellow 42/42, sponge 1/1, envelope 6/4 | Won ✔ |
| level_418 | OK | 7 / 13 | 22 | 198 | 0/0 | yellow 63/61, blue 58/55, coral 39/39, sponge 1/1 | Won ✔ |
| level_419 | OK | 6 / 8 | 18 | 136 | 0/0 | blue 42/41, coral 19/17, purple 38/36, sponge 1/1 | Won ✔ |
| level_420 | OK | 9 / 13 | 22 | 184 | 0/0 | coral 37/37, purple 63/63, yellow 47/47, sponge 1/1 | Won ✔ |
| level_421 | OK | 7 / 11 | 20 | 166 | 0/0 | purple 48/47, yellow 52/52, sponge 1/1 | Won ✔ |
| level_422 | OK | 7 / 10 | 14 | 102 | 0/0 | yellow 23/23, blue 38/38, sponge 1/1 | Won ✔ |
| level_423 | OK | 7 / 9 | 15 | 121 | 0/0 | blue 34/33, coral 35/35, sponge 1/1 | Won ✔ |
| level_424 | OK | 8 / 16 | 20 | 153 | 0/0 | coral 37/37, purple 42/42, yellow 40/40, sponge 1/1 | Won ✔ |
| level_425 | OK | 4 / 10 | 14 | 156 | 0/0 | purple 28/28, yellow 62/61, blue 44/42, sponge 1/1 | Won ✔ |
| level_426 | OK | 8 / 14 | 19 | 152 | 0/0 | yellow 47/47, blue 33/32, coral 37/34, envelope 2/2, sponge 1/1 | Won ✔ |
| level_427 | OK | 9 / 12 | 22 | 171 | 0/0 | blue 42/42, coral 36/36, purple 57/40, sponge 1/1 | Won ✔ |
| level_428 | OK | 18 / 22 | 26 | 134 | 0/0 | coral 29/27, purple 33/32, yellow 31/25, sponge 1/1 | Won ✔ |
| level_429 | OK | 7 / 9 | 18 | 121 | 0/0 | purple 42/41, yellow 23/23, blue 39/29, sponge 1/1 | Won ✔ |
| level_430 | OK | 13 / 15 | 29 | 222 | 0/0 | yellow 54/52, blue 78/60, coral 49/49, sponge 1/1 | Won ✔ |
| level_431 | OK | 6 / 10 | 13 | 114 | 0/0 | blue 32/32, coral 24/23, sponge 2/2 | Won ✔ |
| level_432 | OK | 6 / 9 | 16 | 133 | 0/0 | coral 40/40, purple 37/36, sponge 2/2 | Won ✔ |
| level_433 | OK | 4 / 6 | 15 | 140 | 0/0 | purple 27/25, yellow 25/22, sponge 2/2 | Won ✔ |
| level_434 | OK | 10 / 16 | 23 | 186 | 0/0 | yellow 52/52, blue 62/60, coral 34/34, sponge 2/2 | Won ✔ |
| level_435 | OK | 7 / 15 | 22 | 174 | 0/0 | blue 33/33, coral 54/52, purple 40/36, sponge 2/2 | Won ✔ |
| level_436 | OK | 8 / 12 | 23 | 176 | 0/0 | coral 47/47, purple 66/65, yellow 35/35, sponge 2/2 | Won ✔ |
| level_437 | OK | 9 / 14 | 24 | 192 | 0/0 | purple 73/72, yellow 44/44, blue 60/59, sponge 2/2 | Won ✔ |
| level_438 | OK | 9 / 11 | 20 | 143 | 0/0 | yellow 41/41, blue 45/45, coral 26/22, sponge 2/2 | Won ✔ |
| level_439 | OK | 8 / 14 | 21 | 196 | 0/0 | blue 74/73, coral 42/42, purple 50/49, sponge 2/2 | Won ✔ |
| level_440 | OK | 10 / 13 | 20 | 119 | 0/0 | coral 20/20, purple 49/48, yellow 30/30, sponge 2/2 | Won ✔ |
| level_441 | OK | 8 / 13 | 19 | 147 | 0/0 | purple 51/51, yellow 31/30, sponge 2/2 | Won ✔ |
| level_442 | OK | 9 / 15 | 20 | 152 | 0/0 | yellow 28/27, blue 51/51, sponge 2/2 | Won ✔ |
| level_443 | OK | 9 / 12 | 21 | 154 | 0/0 | blue 37/37, coral 42/42, sponge 2/2 | Won ✔ |
| level_444 | OK | 6 / 10 | 19 | 154 | 0/0 | coral 25/25, purple 34/31, yellow 57/57, sponge 2/2 | Won ✔ |
| level_445 | OK | 7 / 13 | 24 | 190 | 0/0 | purple 86/82, yellow 32/32, blue 45/42, sponge 2/2 | Won ✔ |
| level_446 | OK | 8 / 14 | 23 | 158 | 0/0 | yellow 43/43, blue 28/27, coral 57/41, envelope 2/2, sponge 2/2 | Won ✔ |
| level_447 | OK | 6 / 7 | 16 | 127 | 0/0 | blue 40/37, coral 38/37, purple 22/19, sponge 2/2 | Won ✔ |
| level_448 | OK | 7 / 11 | 21 | 175 | 0/0 | coral 57/57, purple 37/36, yellow 36/36, sponge 2/2 | Won ✔ |
| level_449 | OK | 1 / 1 | 3 | 17 | 0/0 | yellow 11/11 | Won ✔ |
| level_450 | OK | 14 / 16 | 30 | 233 | 0/0 | blue 64/64, coral 74/74, sponge 2/2, envelope 11/4 | Won ✔ |
| level_451 | OK | 11 / 14 | 22 | 156 | 0/0 | blue 49/49, coral 63/63, sponge 2/2 | Won ✔ |
| level_452 | OK | 6 / 11 | 18 | 147 | 0/0 | coral 37/37, purple 48/47, sponge 2/2 | Won ✔ |
| level_453 | OK | 7 / 11 | 18 | 121 | 0/0 | purple 45/44, yellow 22/21, sponge 2/2 | Won ✔ |
| level_454 | OK | 6 / 9 | 18 | 154 | 0/0 | yellow 35/35, blue 55/53, coral 28/27, sponge 2/2 | Won ✔ |
| level_455 | OK | 8 / 11 | 22 | 185 | 0/0 | blue 64/64, coral 47/45, purple 45/37, sponge 2/2 | Won ✔ |
| level_456 | OK | 6 / 14 | 17 | 130 | 0/0 | coral 40/40, purple 26/26, yellow 45/45, sponge 2/2 | Won ✔ |
| level_457 | OK | 8 / 11 | 19 | 158 | 0/0 | yellow 58/58, blue 40/37, sponge 2/2, envelope 10/8 | Won ✔ |
| level_458 | OK | 6 / 6 | 20 | 131 | 0/0 | yellow 41/41, blue 38/38, coral 31/28, sponge 2/2 | Won ✔ |
| level_459 | OK | 6 / 13 | 22 | 177 | 0/0 | blue 48/48, coral 41/41, purple 47/47, sponge 2/2 | Won ✔ |
| level_460 | OK | 8 / 13 | 25 | 231 | 0/0 | coral 72/72, purple 74/73, yellow 37/36, sponge 2/2 | Won ✔ |
| level_461 | OK | 5 / 10 | 16 | 133 | 0/0 | purple 34/34, yellow 45/40, sponge 2/2 | Won ✔ |
| level_462 | OK | 10 / 18 | 26 | 154 | 0/0 | yellow 47/47, blue 38/37, sponge 2/2 | Won ✔ |
| level_463 | OK | 6 / 10 | 14 | 155 | 0/0 | blue 61/61, coral 17/17, sponge 2/2 | Won ✔ |
| level_464 | OK | 9 / 17 | 20 | 179 | 0/0 | coral 46/46, purple 49/47, yellow 30/28, sponge 2/2 | Won ✔ |
| level_465 | OK | 6 / 12 | 22 | 167 | 0/0 | purple 37/37, yellow 40/37, blue 45/41, sponge 2/2 | Won ✔ |
| level_466 | OK | 11 / 19 | 26 | 170 | 0/0 | yellow 30/30, blue 50/50, coral 55/48, envelope 2/2, sponge 2/2 | Won ✔ |
| level_467 | OK | 7 / 12 | 22 | 189 | 0/0 | blue 52/52, coral 33/32, purple 50/47, sponge 2/2 | Won ✔ |
| level_468 | OK | 8 / 12 | 23 | 186 | 0/0 | coral 47/47, purple 58/57, yellow 29/28, sponge 2/2 | Won ✔ |
| level_469 | OK | 6 / 15 | 21 | 199 | 0/0 | purple 43/43, yellow 39/36, blue 64/59, sponge 2/2 | Won ✔ |
| level_470 | OK | 8 / 11 | 21 | 217 | 0/0 | yellow 79/79, blue 49/49, coral 44/43, sponge 2/2 | Won ✔ |
| level_471 | OK | 5 / 9 | 14 | 121 | 0/0 | blue 38/38, coral 24/23, sponge 2/2 | Won ✔ |
| level_472 | OK | 7 / 12 | 18 | 143 | 0/0 | coral 57/57, purple 42/42, sponge 2/2 | Won ✔ |
| level_473 | OK | 8 / 11 | 19 | 134 | 0/0 | purple 58/58, yellow 27/26, sponge 2/2 | Won ✔ |
| level_474 | OK | 10 / 14 | 28 | 170 | 0/0 | yellow 58/57, blue 45/45, coral 34/32, sponge 2/2 | Won ✔ |
| level_475 | OK | 7 / 11 | 21 | 183 | 0/0 | blue 59/59, coral 36/36, purple 60/56, sponge 2/2 | Won ✔ |
| level_476 | OK | 8 / 12 | 20 | 183 | 0/0 | coral 55/55, purple 44/44, yellow 45/41, sponge 2/2 | Won ✔ |
| level_477 | OK | 9 / 12 | 23 | 171 | 0/0 | purple 69/68, yellow 46/46, blue 33/28, sponge 2/2 | Won ✔ |
| level_478 | OK | 8 / 11 | 23 | 186 | 0/0 | yellow 51/50, blue 47/47, coral 49/47, sponge 2/2 | Won ✔ |
| level_479 | OK | 8 / 12 | 23 | 189 | 0/0 | blue 49/48, coral 50/49, purple 56/53, sponge 2/2 | Won ✔ |
| level_480 | OK | 8 / 11 | 22 | 213 | 0/0 | coral 72/72, purple 46/46, yellow 49/49, sponge 2/2 | Won ✔ |
| level_481 | OK | 6 / 13 | 16 | 174 | 0/0 | purple 43/43, yellow 63/63, sponge 2/2 | Won ✔ |
| level_482 | OK | 8 / 15 | 20 | 144 | 0/0 | yellow 54/53, blue 27/27, sponge 2/2 | Won ✔ |
| level_483 | OK | 5 / 9 | 13 | 92 | 0/0 | blue 34/34, coral 36/33, sponge 2/2 | Won ✔ |
| level_484 | OK | 9 / 15 | 27 | 188 | 0/0 | coral 62/59, purple 29/28, yellow 50/50, sponge 2/2 | Won ✔ |
| level_485 | OK | 7 / 9 | 18 | 145 | 0/0 | purple 26/26, yellow 47/45, blue 30/29, sponge 2/2 | Won ✔ |
| level_486 | OK | 7 / 11 | 24 | 181 | 0/0 | yellow 46/46, blue 57/56, coral 34/32, envelope 2/2, sponge 2/2 | Won ✔ |
| level_487 | OK | 8 / 13 | 23 | 164 | 0/0 | blue 43/43, coral 42/42, purple 45/45, sponge 2/2 | Won ✔ |
| level_488 | OK | 7 / 9 | 20 | 136 | 0/0 | coral 31/31, purple 36/36, yellow 47/41, sponge 2/2 | Won ✔ |
| level_489 | OK | 8 / 11 | 21 | 176 | 0/0 | purple 26/26, yellow 76/76, blue 62/60, sponge 2/2 | Won ✔ |
| level_490 | OK | 6 / 12 | 17 | 204 | 0/0 | blue 71/71, coral 44/40, sponge 2/2, envelope 9/8 | Won ✔ |
| level_491 | OK | 6 / 11 | 13 | 118 | 0/0 | blue 47/47, coral 35/34, sponge 2/2 | Won ✔ |
| level_492 | OK | 8 / 17 | 21 | 133 | 0/0 | coral 35/35, purple 32/32, sponge 2/2 | Won ✔ |
| level_493 | OK | 11 / 16 | 18 | 109 | 0/0 | purple 29/29, yellow 20/17, sponge 2/2 | Won ✔ |
| level_494 | OK | 7 / 11 | 20 | 160 | 0/0 | yellow 55/54, blue 41/40, coral 33/31, sponge 2/2 | Won ✔ |
| level_495 | OK | 8 / 12 | 25 | 185 | 0/0 | blue 40/40, coral 38/38, purple 55/49, sponge 2/2 | Won ✔ |
| level_496 | OK | 8 / 10 | 24 | 218 | 0/0 | coral 51/51, purple 61/59, yellow 45/42, sponge 2/2 | Won ✔ |
| level_497 | OK | 9 / 17 | 25 | 207 | 0/0 | purple 64/62, yellow 54/54, blue 56/56, sponge 2/2 | Won ✔ |
| level_498 | OK | 6 / 7 | 18 | 126 | 0/0 | yellow 27/27, blue 29/26, coral 41/30, sponge 2/2 | Won ✔ |
| level_499 | OK | 1 / 1 | 3 | 11 | 0/0 | coral 8/8 | Won ✔ |
| level_500 | OK | 8 / 13 | 20 | 195 | 0/0 | coral 49/49, purple 45/44, yellow 55/55, sponge 2/2 | Won ✔ |
| level_501 | OK | 5 / 12 | 16 | 129 | 0/0 | purple 57/57, yellow 40/40 | Won ✔ |
| level_502 | OK | 7 / 12 | 17 | 147 | 0/0 | yellow 46/46, blue 36/31 | Won ✔ |
| level_503 | OK | 8 / 11 | 17 | 139 | 0/0 | blue 34/34, coral 34/30 | Won ✔ |
| level_504 | OK | 7 / 15 | 19 | 153 | 0/0 | coral 24/22, purple 43/43, yellow 36/36 | Won ✔ |
| level_505 | OK | 9 / 14 | 18 | 146 | 0/0 | purple 28/28, yellow 33/30, blue 46/42 | Won ✔ |
| level_506 | OK | 9 / 17 | 24 | 192 | 0/0 | yellow 54/54, blue 51/51, coral 48/40, envelope 2/2 | Won ✔ |
| level_507 | OK | 6 / 10 | 19 | 152 | 0/0 | blue 52/52, coral 24/23, purple 36/36 | Won ✔ |
| level_508 | OK | 13 / 18 | 20 | 136 | 0/0 | coral 34/34, purple 38/38, yellow 51/49 | Won ✔ |
| level_509 | OK | 7 / 10 | 20 | 183 | 0/0 | purple 44/44, yellow 63/59, blue 28/23 | Won ✔ |
| level_510 | OK | 7 / 11 | 23 | 204 | 0/0 | yellow 64/63, blue 59/58, coral 49/48 | Won ✔ |
| level_511 | OK | 6 / 9 | 16 | 145 | 0/0 | blue 32/32, coral 35/27 | Won ✔ |
| level_512 | OK | 10 / 13 | 24 | 150 | 0/0 | coral 57/57, purple 38/38 | Won ✔ |
| level_513 | OK | 5 / 7 | 17 | 121 | 0/0 | purple 27/27, yellow 37/36 | Won ✔ |
| level_514 | OK | 9 / 17 | 19 | 166 | 0/0 | yellow 38/38, blue 53/53, coral 56/52 | Won ✔ |
| level_515 | OK | 7 / 10 | 20 | 183 | 0/0 | blue 45/45, coral 62/62, purple 41/40 | Won ✔ |
| level_516 | OK | 9 / 12 | 20 | 179 | 0/0 | coral 58/58, purple 52/48, yellow 39/33 | Won ✔ |
| level_517 | OK | 6 / 10 | 18 | 188 | 0/0 | purple 27/27, yellow 52/51, blue 57/55 | Won ✔ |
| level_518 | OK | 8 / 13 | 21 | 197 | 0/0 | yellow 71/71, blue 54/54, coral 22/21 | Won ✔ |
| level_519 | OK | 10 / 12 | 23 | 171 | 0/0 | blue 70/70, coral 26/26, purple 30/30 | Won ✔ |
| level_520 | OK | 9 / 16 | 22 | 151 | 0/0 | coral 58/58, purple 34/33, yellow 46/40 | Won ✔ |
| level_521 | OK | 7 / 13 | 18 | 158 | 0/0 | purple 52/52, yellow 36/33 | Won ✔ |
| level_522 | OK | 7 / 12 | 15 | 135 | 0/0 | yellow 46/46, blue 37/37 | Won ✔ |
| level_523 | OK | 7 / 10 | 17 | 125 | 0/0 | blue 43/43, coral 21/20 | Won ✔ |
| level_524 | OK | 6 / 10 | 19 | 163 | 0/0 | coral 39/39, purple 48/48, yellow 36/36 | Won ✔ |
| level_525 | OK | 8 / 16 | 24 | 167 | 0/0 | purple 44/44, yellow 30/30, blue 59/59 | Won ✔ |
| level_526 | OK | 6 / 10 | 18 | 150 | 0/0 | yellow 39/39, blue 39/34, coral 38/37, envelope 2/2 | Won ✔ |
| level_527 | OK | 6 / 10 | 20 | 193 | 0/0 | blue 47/47, coral 59/57, purple 52/51 | Won ✔ |
| level_528 | OK | 6 / 9 | 17 | 172 | 0/0 | coral 38/37, purple 37/37, yellow 58/52 | Won ✔ |
| level_529 | OK | 7 / 10 | 22 | 172 | 0/0 | purple 37/37, yellow 53/53, blue 49/49 | Won ✔ |
| level_530 | OK | 8 / 13 | 23 | 215 | 0/0 | yellow 39/39, blue 60/60, envelope 10/4 | Won ✔ |
| level_531 | OK | 6 / 10 | 15 | 120 | 0/0 | blue 50/50, coral 21/21 | Won ✔ |
| level_532 | OK | 4 / 8 | 15 | 123 | 0/0 | coral 50/50, purple 31/30 | Won ✔ |
| level_533 | OK | 10 / 12 | 23 | 154 | 0/0 | purple 52/52, yellow 38/38 | Won ✔ |
| level_534 | OK | 8 / 15 | 25 | 149 | 0/0 | yellow 40/40, blue 38/38, coral 32/29 | Won ✔ |
| level_535 | OK | 6 / 16 | 18 | 181 | 0/0 | blue 47/45, coral 49/48, purple 44/41 | Won ✔ |
| level_536 | OK | 8 / 14 | 27 | 225 | 0/0 | coral 75/74, purple 41/40, yellow 42/41 | Won ✔ |
| level_537 | OK | 7 / 11 | 17 | 115 | 0/0 | purple 33/33, yellow 43/41, envelope 7/7 | Won ✔ |
| level_538 | OK | 13 / 16 | 23 | 138 | 0/0 | yellow 42/42, blue 31/31, coral 30/30 | Won ✔ |
| level_539 | OK | 9 / 11 | 23 | 160 | 0/0 | blue 30/30, coral 64/63, purple 37/37 | Won ✔ |
| level_540 | OK | 17 / 20 | 27 | 174 | 0/0 | coral 36/34, purple 46/46, yellow 43/42 | Won ✔ |
| level_541 | OK | 9 / 13 | 24 | 159 | 0/0 | purple 66/66, yellow 55/50 | Won ✔ |
| level_542 | OK | 4 / 10 | 14 | 134 | 0/0 | yellow 39/38, blue 54/54 | Won ✔ |
| level_543 | OK | 9 / 15 | 21 | 133 | 0/0 | blue 43/43, coral 36/36 | Won ✔ |
| level_544 | OK | 9 / 17 | 22 | 170 | 0/0 | coral 53/52, purple 42/41, yellow 35/29 | Won ✔ |
| level_545 | OK | 8 / 18 | 23 | 174 | 0/0 | purple 62/61, yellow 44/42, blue 32/32 | Won ✔ |
| level_546 | OK | 8 / 12 | 21 | 117 | 0/0 | yellow 34/32, blue 37/36, coral 24/23, envelope 2/2 | Won ✔ |
| level_547 | OK | 6 / 8 | 18 | 156 | 0/0 | blue 47/44, coral 44/43, purple 45/37 | Won ✔ |
| level_548 | OK | 17 / 22 | 28 | 143 | 0/0 | coral 50/44, purple 35/35, yellow 38/37 | Won ✔ |
| level_549 | OK | 1 / 1 | 3 | 15 | 0/0 | coral 10/10 | Won ✔ |
| level_550 | OK | 10 / 14 | 26 | 234 | 0/0 | yellow 65/64, blue 60/60, coral 47/47 | Won ✔ |
| level_551 | OK | 6 / 11 | 16 | 143 | 0/0 | blue 36/36, coral 46/46 | Won ✔ |
| level_552 | OK | 6 / 16 | 13 | 92 | 0/0 | coral 47/42, purple 31/30 | Won ✔ |
| level_553 | OK | 7 / 12 | 17 | 133 | 0/0 | purple 36/35, yellow 34/34 | Won ✔ |
| level_554 | OK | 10 / 15 | 26 | 187 | 0/0 | yellow 47/47, blue 46/46, coral 54/51 | Won ✔ |
| level_555 | OK | 10 / 16 | 24 | 166 | 0/0 | blue 53/53, coral 32/32, purple 30/29 | Won ✔ |
| level_556 | OK | 6 / 16 | 22 | 209 | 0/0 | coral 67/67, purple 50/49, yellow 49/46 | Won ✔ |
| level_557 | OK | 6 / 7 | 19 | 155 | 0/0 | purple 53/53, yellow 42/42, blue 45/45 | Won ✔ |
| level_558 | OK | 6 / 13 | 23 | 183 | 0/0 | yellow 45/44, blue 39/39, coral 40/39 | Won ✔ |
| level_559 | OK | 5 / 9 | 18 | 196 | 0/0 | blue 52/51, coral 41/40, purple 46/44 | Won ✔ |
| level_560 | OK | 9 / 14 | 26 | 161 | 0/0 | coral 44/44, purple 55/53, yellow 40/40 | Won ✔ |
| level_561 | OK | 7 / 11 | 19 | 125 | 0/0 | purple 29/29, yellow 33/33 | Won ✔ |
| level_562 | OK | 6 / 9 | 18 | 153 | 0/0 | yellow 38/38, blue 36/35 | Won ✔ |
| level_563 | OK | 9 / 16 | 22 | 163 | 0/0 | blue 40/38, coral 44/44 | Won ✔ |
| level_564 | OK | 8 / 13 | 20 | 141 | 0/0 | coral 35/35, purple 26/25, yellow 44/44 | Won ✔ |
| level_565 | OK | 9 / 17 | 21 | 168 | 0/0 | purple 43/43, yellow 43/42, blue 41/41 | Won ✔ |
| level_566 | OK | 9 / 13 | 23 | 187 | 0/0 | yellow 58/57, blue 61/60, coral 23/22, envelope 2/2 | Won ✔ |
| level_567 | OK | 9 / 11 | 22 | 181 | 0/0 | blue 54/54, coral 41/41, purple 50/43 | Won ✔ |
| level_568 | OK | 7 / 11 | 22 | 194 | 0/0 | coral 56/56, purple 52/45, yellow 46/44 | Won ✔ |
| level_569 | OK | 5 / 11 | 17 | 155 | 0/0 | purple 30/30, yellow 34/34, blue 38/37 | Won ✔ |
| level_570 | OK | 5 / 7 | 20 | 161 | 0/0 | blue 46/45, coral 39/39, envelope 15/10 | Won ✔ |
| level_571 | OK | 5 / 8 | 13 | 132 | 0/0 | blue 48/48, coral 35/35 | Won ✔ |
| level_572 | OK | 11 / 15 | 23 | 155 | 0/0 | coral 61/60, purple 25/25 | Won ✔ |
| level_573 | OK | 9 / 12 | 25 | 160 | 0/0 | purple 36/36, yellow 33/33 | Won ✔ |
| level_574 | OK | 9 / 12 | 24 | 175 | 0/0 | yellow 31/31, blue 65/65, coral 46/45 | Won ✔ |
| level_575 | OK | 9 / 15 | 24 | 193 | 0/0 | blue 58/57, coral 68/68, purple 46/46 | Won ✔ |
| level_576 | OK | 7 / 10 | 22 | 163 | 0/0 | coral 43/43, purple 55/53, yellow 31/30 | Won ✔ |
| level_577 | OK | 9 / 11 | 24 | 168 | 0/0 | purple 58/58, yellow 33/33, blue 49/44 | Won ✔ |
| level_578 | OK | 6 / 10 | 17 | 117 | 0/0 | yellow 28/28, blue 48/44, coral 31/31 | Won ✔ |
| level_579 | OK | 7 / 13 | 23 | 165 | 0/0 | blue 32/32, coral 51/51, purple 53/51 | Won ✔ |
| level_580 | OK | 22 / 25 | 35 | 203 | 0/0 | coral 59/57, purple 46/46, yellow 47/47 | Won ✔ |
| level_581 | OK | 6 / 13 | 18 | 147 | 0/0 | purple 48/47, yellow 32/32 | Won ✔ |
| level_582 | OK | 8 / 14 | 23 | 148 | 0/0 | yellow 50/50, blue 35/35 | Won ✔ |
| level_583 | OK | 9 / 15 | 22 | 136 | 0/0 | blue 37/37, coral 38/38 | Won ✔ |
| level_584 | OK | 7 / 11 | 22 | 184 | 0/0 | coral 57/57, purple 52/51, yellow 29/28 | Won ✔ |
| level_585 | OK | 7 / 11 | 21 | 167 | 0/0 | purple 32/32, yellow 51/48, blue 41/38 | Won ✔ |
| level_586 | OK | 8 / 14 | 22 | 166 | 0/0 | yellow 52/51, blue 30/30, coral 59/57, envelope 2/2 | Won ✔ |
| level_587 | OK | 8 / 12 | 25 | 193 | 0/0 | blue 53/51, coral 64/55, purple 32/30 | Won ✔ |
| level_588 | OK | 8 / 12 | 22 | 147 | 0/0 | coral 43/43, purple 41/39, yellow 32/23 | Won ✔ |
| level_589 | OK | 8 / 12 | 24 | 211 | 0/0 | purple 57/57, yellow 53/52, blue 44/44 | Won ✔ |
| level_590 | OK | 8 / 11 | 22 | 184 | 0/0 | yellow 58/58, blue 59/58, coral 46/39 | Won ✔ |
| level_591 | OK | 5 / 9 | 13 | 102 | 0/0 | blue 38/34, coral 18/18 | Won ✔ |
| level_592 | OK | 7 / 11 | 17 | 157 | 0/0 | coral 53/53, purple 51/51 | Won ✔ |
| level_593 | OK | 6 / 10 | 18 | 139 | 0/0 | purple 33/33, yellow 41/41 | Won ✔ |
| level_594 | OK | 8 / 11 | 24 | 189 | 0/0 | yellow 29/29, blue 49/49, coral 58/56 | Won ✔ |
| level_595 | OK | 6 / 12 | 17 | 176 | 0/0 | blue 57/57, coral 43/41, purple 41/38 | Won ✔ |
| level_596 | OK | 9 / 15 | 22 | 203 | 0/0 | coral 73/72, purple 68/68, yellow 45/45 | Won ✔ |
| level_597 | OK | 9 / 15 | 20 | 157 | 0/0 | purple 56/56, yellow 26/26, blue 54/54 | Won ✔ |
| level_598 | OK | 6 / 10 | 18 | 169 | 0/0 | yellow 56/56, blue 39/38, coral 49/42 | Won ✔ |
| level_599 | OK | 1 / 1 | 3 | 20 | 0/0 | blue 14/14 | Won ✔ |
| level_600 | OK | 8 / 15 | 16 | 136 | 0/0 | coral 41/41, yellow 52/48, envelope 6/4 | Won ✔ |
| level_601 | OK | 7 / 10 | 21 | 144 | 0/0 | purple 50/50, yellow 48/47 | Won ✔ |
| level_602 | OK | 10 / 19 | 24 | 141 | 0/0 | yellow 39/39, blue 31/28 | Won ✔ |
| level_603 | OK | 6 / 10 | 18 | 135 | 0/0 | blue 36/36, coral 27/24 | Won ✔ |
| level_604 | OK | 7 / 12 | 17 | 156 | 0/0 | coral 57/57, purple 33/32, yellow 55/55 | Won ✔ |
| level_605 | OK | 6 / 10 | 16 | 143 | 0/0 | purple 49/49, yellow 22/22, blue 46/45 | Won ✔ |
| level_606 | OK | 8 / 12 | 23 | 140 | 0/0 | yellow 41/40, blue 37/33, coral 39/37, envelope 2/2 | Won ✔ |
| level_607 | OK | 7 / 10 | 19 | 175 | 0/0 | blue 64/63, coral 22/22, purple 51/46 | Won ✔ |
| level_608 | OK | 7 / 8 | 19 | 179 | 0/0 | coral 54/54, purple 43/43, yellow 62/50 | Won ✔ |
| level_609 | OK | 6 / 13 | 19 | 151 | 0/0 | purple 42/42, yellow 34/30, blue 43/40 | Won ✔ |
| level_610 | OK | 9 / 17 | 25 | 197 | 0/0 | yellow 51/51, blue 59/57, envelope 4/3 | Won ✔ |
| level_611 | OK | 6 / 16 | 17 | 121 | 0/0 | blue 23/23, coral 47/46 | Won ✔ |
| level_612 | OK | 7 / 10 | 20 | 152 | 0/0 | coral 46/46, purple 47/46 | Won ✔ |
| level_613 | OK | 9 / 11 | 20 | 158 | 0/0 | purple 55/55, yellow 23/23 | Won ✔ |
| level_614 | OK | 9 / 11 | 26 | 192 | 0/0 | yellow 58/57, blue 28/28, coral 63/57 | Won ✔ |
| level_615 | OK | 7 / 15 | 22 | 162 | 0/0 | blue 56/56, coral 29/27, purple 39/36 | Won ✔ |
| level_616 | OK | 6 / 10 | 23 | 179 | 0/0 | coral 55/53, purple 36/36, yellow 38/34 | Won ✔ |
| level_617 | OK | 10 / 18 | 23 | 177 | 0/0 | purple 42/42, blue 63/60, envelope 4/3 | Won ✔ |
| level_618 | OK | 9 / 17 | 22 | 110 | 0/0 | yellow 38/37, blue 26/24, coral 18/13 | Won ✔ |
| level_619 | OK | 8 / 12 | 22 | 160 | 0/0 | blue 38/38, coral 54/54, purple 47/47 | Won ✔ |
| level_620 | OK | 15 / 21 | 23 | 137 | 0/0 | coral 55/52, purple 39/38, yellow 17/17 | Won ✔ |
| level_621 | OK | 10 / 20 | 23 | 155 | 0/0 | purple 61/61, yellow 42/42 | Won ✔ |
| level_622 | OK | 6 / 9 | 15 | 114 | 0/0 | yellow 47/44, blue 29/29 | Won ✔ |
| level_623 | OK | 10 / 14 | 22 | 142 | 0/0 | blue 41/41, coral 46/44 | Won ✔ |
| level_624 | OK | 6 / 11 | 19 | 140 | 0/0 | coral 31/31, purple 42/41, yellow 36/35 | Won ✔ |
| level_625 | OK | 6 / 16 | 20 | 163 | 0/0 | purple 54/52, yellow 39/38, blue 35/32 | Won ✔ |
| level_626 | OK | 8 / 12 | 23 | 160 | 0/0 | yellow 37/37, blue 42/42, coral 49/48, envelope 2/2 | Won ✔ |
| level_627 | OK | 6 / 9 | 17 | 145 | 0/0 | blue 47/46, coral 37/37, purple 24/24 | Won ✔ |
| level_628 | OK | 11 / 14 | 17 | 104 | 0/0 | coral 32/32, purple 30/29, yellow 28/25 | Won ✔ |
| level_629 | OK | 6 / 8 | 19 | 166 | 0/0 | purple 46/46, yellow 44/43, blue 47/47 | Won ✔ |
| level_630 | OK | 7 / 11 | 22 | 228 | 0/0 | yellow 32/32, blue 64/64, coral 58/55 | Won ✔ |
| level_631 | OK | 5 / 8 | 12 | 115 | 0/0 | blue 46/44, coral 33/28 | Won ✔ |
| level_632 | OK | 7 / 10 | 18 | 122 | 0/0 | coral 43/43, purple 34/34 | Won ✔ |
| level_633 | OK | 11 / 15 | 22 | 126 | 0/0 | purple 20/20, yellow 43/43 | Won ✔ |
| level_634 | OK | 9 / 13 | 17 | 152 | 0/0 | yellow 42/42, blue 28/28, coral 35/34 | Won ✔ |
| level_635 | OK | 6 / 14 | 20 | 194 | 0/0 | blue 60/56, coral 57/57, purple 47/47 | Won ✔ |
| level_636 | OK | 8 / 13 | 20 | 147 | 0/0 | coral 34/34, purple 42/42, yellow 26/23 | Won ✔ |
| level_637 | OK | 6 / 12 | 21 | 151 | 0/0 | purple 47/46, yellow 38/37, blue 44/44 | Won ✔ |
| level_638 | OK | 8 / 11 | 21 | 193 | 0/0 | yellow 90/90, blue 47/47, coral 32/32 | Won ✔ |
| level_639 | OK | 14 / 20 | 25 | 202 | 0/0 | blue 67/66, coral 37/37, purple 41/38 | Won ✔ |
| level_640 | OK | 9 / 12 | 25 | 164 | 0/0 | coral 36/36, purple 42/42, yellow 67/57 | Won ✔ |
| level_641 | OK | 9 / 14 | 21 | 146 | 0/0 | purple 31/31, yellow 37/37 | Won ✔ |
| level_642 | OK | 12 / 16 | 28 | 153 | 0/0 | yellow 32/31, blue 41/38 | Won ✔ |
| level_643 | OK | 12 / 15 | 24 | 161 | 0/0 | blue 47/47, coral 38/38 | Won ✔ |
| level_644 | OK | 7 / 11 | 20 | 168 | 0/0 | coral 30/30, purple 39/39, yellow 31/31 | Won ✔ |
| level_645 | OK | 9 / 14 | 25 | 187 | 0/0 | purple 48/48, yellow 51/50, blue 30/23 | Won ✔ |
| level_646 | OK | 12 / 19 | 28 | 174 | 0/0 | yellow 38/36, blue 31/31, coral 58/52, envelope 2/2 | Won ✔ |
| level_647 | OK | 7 / 10 | 23 | 183 | 0/0 | blue 51/51, coral 36/36, purple 58/40 | Won ✔ |
| level_648 | OK | 7 / 11 | 22 | 185 | 0/0 | coral 25/23, purple 30/30, yellow 49/47 | Won ✔ |
| level_649 | OK | 1 / 1 | 3 | 17 | 0/0 | coral 11/11 | Won ✔ |
| level_650 | OK | 11 / 12 | 27 | 206 | 0/0 | blue 45/45, coral 54/47, envelope 12/4 | Won ✔ |
| level_651 | OK | 7 / 15 | 16 | 93 | 0/0 | blue 33/32, coral 29/29 | Won ✔ |
| level_652 | OK | 5 / 8 | 15 | 131 | 0/0 | coral 52/52, purple 31/31 | Won ✔ |
| level_653 | OK | 9 / 13 | 19 | 142 | 0/0 | purple 51/51, yellow 37/37 | Won ✔ |
| level_654 | OK | 5 / 13 | 22 | 154 | 0/0 | yellow 34/33, blue 64/62, coral 25/21 | Won ✔ |
| level_655 | OK | 6 / 9 | 22 | 186 | 0/0 | blue 52/52, coral 26/25, purple 38/31 | Won ✔ |
| level_656 | OK | 7 / 17 | 23 | 193 | 0/0 | coral 50/50, purple 55/55, yellow 63/63 | Won ✔ |
| level_657 | OK | 7 / 11 | 24 | 166 | 0/0 | purple 43/42, yellow 25/25, blue 36/35 | Won ✔ |
| level_658 | OK | 9 / 13 | 20 | 146 | 0/0 | yellow 48/47, blue 35/33, coral 26/23 | Won ✔ |
| level_659 | OK | 7 / 15 | 19 | 174 | 0/0 | blue 51/49, coral 56/56, purple 47/47 | Won ✔ |
| level_660 | OK | 8 / 14 | 25 | 201 | 0/0 | coral 48/47, purple 76/76, yellow 37/37 | Won ✔ |
| level_661 | OK | 5 / 9 | 12 | 113 | 0/0 | purple 34/34, yellow 28/27 | Won ✔ |
| level_662 | OK | 7 / 10 | 20 | 145 | 0/0 | yellow 20/20, blue 28/28 | Won ✔ |
| level_663 | OK | 8 / 12 | 23 | 145 | 0/0 | blue 43/43, coral 39/36 | Won ✔ |
| level_664 | OK | 7 / 10 | 22 | 162 | 0/0 | coral 55/55, purple 53/49, yellow 43/37 | Won ✔ |
| level_665 | OK | 7 / 9 | 21 | 154 | 0/0 | purple 43/42, yellow 51/51, blue 25/25 | Won ✔ |
| level_666 | OK | 9 / 15 | 25 | 152 | 0/0 | yellow 41/40, blue 34/33, coral 32/31, envelope 2/2 | Won ✔ |
| level_667 | OK | 11 / 18 | 24 | 178 | 0/0 | blue 47/45, coral 41/41, purple 41/37 | Won ✔ |
| level_668 | OK | 12 / 12 | 23 | 141 | 0/0 | coral 37/36, purple 33/30, yellow 49/47 | Won ✔ |
| level_669 | OK | 7 / 14 | 22 | 124 | 0/0 | purple 41/41, yellow 31/31, blue 23/21 | Won ✔ |
| level_670 | OK | 11 / 16 | 28 | 204 | 0/0 | yellow 46/42, blue 42/42, coral 61/40 | Won ✔ |
| level_671 | OK | 5 / 10 | 18 | 136 | 0/0 | blue 41/38, coral 48/46 | Won ✔ |
| level_672 | OK | 7 / 13 | 20 | 160 | 0/0 | coral 52/52, purple 32/32 | Won ✔ |
| level_673 | OK | 6 / 12 | 18 | 150 | 0/0 | purple 34/34, yellow 45/45 | Won ✔ |
| level_674 | OK | 8 / 13 | 21 | 176 | 0/0 | yellow 55/55, blue 63/61, coral 31/31 | Won ✔ |
| level_675 | OK | 9 / 20 | 22 | 167 | 0/0 | blue 32/32, coral 39/38, purple 46/42 | Won ✔ |
| level_676 | OK | 7 / 13 | 25 | 186 | 0/0 | coral 51/51, purple 40/39, yellow 51/51 | Won ✔ |
| level_677 | OK | 10 / 14 | 29 | 208 | 0/0 | purple 34/34, yellow 46/45, blue 79/77 | Won ✔ |
| level_678 | OK | 6 / 9 | 21 | 167 | 0/0 | yellow 44/44, blue 30/30, coral 43/38 | Won ✔ |
| level_679 | OK | 7 / 15 | 24 | 188 | 0/0 | blue 54/54, coral 45/45, purple 54/49 | Won ✔ |
| level_680 | OK | 8 / 14 | 26 | 211 | 0/0 | coral 75/75, purple 64/64, yellow 31/26 | Won ✔ |
| level_681 | OK | 7 / 17 | 18 | 132 | 0/0 | purple 43/43, yellow 39/39 | Won ✔ |
| level_682 | OK | 6 / 11 | 22 | 163 | 0/0 | yellow 52/52, blue 37/34 | Won ✔ |
| level_683 | OK | 11 / 19 | 23 | 132 | 0/0 | blue 35/35, coral 18/18 | Won ✔ |
| level_684 | OK | 6 / 8 | 19 | 162 | 0/0 | coral 60/58, purple 45/40, yellow 42/42 | Won ✔ |
| level_685 | OK | 6 / 8 | 16 | 174 | 0/0 | purple 21/21, yellow 70/70, blue 41/40 | Won ✔ |
| level_686 | OK | 8 / 10 | 21 | 184 | 0/0 | yellow 90/90, blue 34/34, coral 25/25, envelope 2/2 | Won ✔ |
| level_687 | OK | 11 / 14 | 24 | 189 | 0/0 | blue 33/33, coral 84/82, purple 32/32 | Won ✔ |
| level_688 | OK | 7 / 11 | 21 | 169 | 0/0 | coral 51/51, purple 39/35, yellow 35/30 | Won ✔ |
| level_689 | OK | 9 / 17 | 23 | 191 | 0/0 | purple 76/76, yellow 34/34, blue 43/43 | Won ✔ |
| level_690 | OK | 13 / 14 | 30 | 201 | 0/0 | blue 47/47, coral 52/48, envelope 18/10 | Won ✔ |
| level_691 | OK | 10 / 14 | 19 | 150 | 0/0 | blue 60/60, coral 36/36 | Won ✔ |
| level_692 | OK | 7 / 12 | 18 | 149 | 0/0 | coral 35/35, purple 32/32 | Won ✔ |
| level_693 | OK | 12 / 16 | 22 | 119 | 0/0 | purple 30/30, yellow 32/32 | Won ✔ |
| level_694 | OK | 8 / 15 | 25 | 168 | 0/0 | yellow 51/51, blue 48/47, coral 34/31 | Won ✔ |
| level_695 | OK | 7 / 13 | 24 | 176 | 0/0 | blue 43/42, coral 52/50, purple 46/45 | Won ✔ |
| level_696 | OK | 8 / 13 | 22 | 192 | 0/0 | coral 65/65, purple 43/43, yellow 43/42 | Won ✔ |
| level_697 | OK | 8 / 11 | 24 | 174 | 0/0 | purple 38/38, yellow 31/31, blue 44/41 | Won ✔ |
| level_698 | OK | 6 / 12 | 20 | 139 | 0/0 | yellow 55/55, blue 26/25, coral 30/30 | Won ✔ |
| level_699 | OK | 1 / 1 | 3 | 18 | 0/0 | blue 14/14 | Won ✔ |
| level_700 | OK | 10 / 15 | 17 | 118 | 0/0 | coral 32/30, purple 38/38, yellow 28/24 | Won ✔ |
| level_701 | OK | 6 / 10 | 15 | 126 | 0/0 | purple 33/33, yellow 38/38 | Won ✔ |
| level_702 | OK | 6 / 11 | 16 | 114 | 0/0 | yellow 32/32, blue 41/41 | Won ✔ |
| level_703 | OK | 10 / 14 | 20 | 132 | 0/0 | blue 26/26, coral 53/53 | Won ✔ |
| level_704 | OK | 11 / 14 | 24 | 184 | 0/0 | coral 42/42, purple 54/54, yellow 47/46, sponge 1/1 | Won ✔ |
| level_705 | OK | 9 / 19 | 19 | 147 | 0/0 | purple 37/37, yellow 42/42, blue 35/24, sponge 1/1 | Won ✔ |
| level_706 | OK | 10 / 20 | 27 | 167 | 0/0 | yellow 47/46, blue 51/51, coral 41/41, envelope 2/2, sponge 1/1 | Won ✔ |
| level_707 | OK | 6 / 11 | 19 | 179 | 0/0 | blue 48/48, coral 55/55, purple 27/27, sponge 1/1 | Won ✔ |
| level_708 | OK | 9 / 15 | 20 | 169 | 0/0 | coral 68/68, purple 45/45, yellow 29/29, sponge 1/1 | Won ✔ |
| level_709 | OK | 7 / 10 | 20 | 207 | 0/0 | purple 52/52, yellow 34/32, blue 51/39, sponge 1/1 | Won ✔ |
| level_710 | OK | 7 / 9 | 19 | 196 | 0/0 | yellow 59/59, blue 40/40, coral 46/42 | Won ✔ |
| level_711 | OK | 8 / 11 | 18 | 133 | 0/0 | blue 39/39, coral 37/37 | Won ✔ |
| level_712 | OK | 8 / 11 | 18 | 162 | 0/0 | coral 33/33, purple 38/38 | Won ✔ |
| level_713 | OK | 9 / 11 | 17 | 154 | 0/0 | purple 54/53, yellow 32/31 | Won ✔ |
| level_714 | OK | 8 / 10 | 23 | 169 | 0/0 | yellow 52/52, blue 46/46, coral 46/44, sponge 1/1 | Won ✔ |
| level_715 | OK | 6 / 16 | 18 | 147 | 0/0 | blue 42/39, coral 34/34, purple 43/41, sponge 1/1 | Won ✔ |
| level_716 | OK | 9 / 13 | 24 | 188 | 0/0 | coral 71/69, purple 52/52, yellow 35/35, sponge 1/1 | Won ✔ |
| level_717 | OK | 8 / 12 | 28 | 144 | 0/0 | purple 34/34, yellow 45/45, blue 30/28, sponge 1/1 | Won ✔ |
| level_718 | OK | 8 / 11 | 23 | 195 | 0/0 | yellow 49/48, blue 32/32, coral 48/45, sponge 1/1 | Won ✔ |
| level_719 | OK | 9 / 13 | 19 | 161 | 0/0 | blue 32/32, coral 43/43, purple 41/41, sponge 1/1 | Won ✔ |
| level_720 | OK | 10 / 16 | 20 | 162 | 0/0 | coral 40/40, purple 43/43, yellow 46/39 | Won ✔ |
| level_721 | OK | 9 / 14 | 23 | 158 | 0/0 | purple 40/40, yellow 50/50 | Won ✔ |
| level_722 | OK | 10 / 13 | 19 | 150 | 0/0 | yellow 46/45, blue 52/52 | Won ✔ |
| level_723 | OK | 6 / 8 | 17 | 130 | 0/0 | blue 37/37, coral 39/39 | Won ✔ |
| level_724 | OK | 8 / 16 | 22 | 185 | 0/0 | coral 47/47, purple 84/78, yellow 24/24, sponge 1/1 | Won ✔ |
| level_725 | OK | 6 / 10 | 19 | 186 | 0/0 | purple 38/37, yellow 66/63, blue 45/42, sponge 1/1 | Won ✔ |
| level_726 | OK | 7 / 10 | 21 | 158 | 0/0 | yellow 46/46, blue 54/49, coral 29/27, envelope 2/2, sponge 1/1 | Won ✔ |
| level_727 | OK | 6 / 9 | 19 | 153 | 0/0 | blue 58/58, coral 32/30, purple 29/25, sponge 1/1 | Won ✔ |
| level_728 | OK | 6 / 10 | 20 | 183 | 0/0 | coral 53/53, purple 47/47, yellow 28/28, sponge 1/1 | Won ✔ |
| level_729 | OK | 9 / 15 | 20 | 185 | 0/0 | purple 40/40, yellow 43/42, blue 58/54, sponge 1/1 | Won ✔ |
| level_730 | OK | 17 / 18 | 33 | 232 | 0/0 | yellow 58/58, coral 37/33, envelope 7/7 | Won ✔ |
| level_731 | OK | 6 / 9 | 15 | 130 | 0/0 | blue 38/38, coral 32/32 | Won ✔ |
| level_732 | OK | 7 / 12 | 21 | 136 | 0/0 | coral 40/40, purple 33/31 | Won ✔ |
| level_733 | OK | 8 / 12 | 16 | 112 | 0/0 | purple 33/33, yellow 35/35 | Won ✔ |
| level_734 | OK | 7 / 12 | 22 | 170 | 0/0 | yellow 42/41, blue 42/42, coral 47/47, sponge 1/1 | Won ✔ |
| level_735 | OK | 7 / 12 | 18 | 130 | 0/0 | blue 28/28, coral 42/40, purple 27/27, sponge 1/1 | Won ✔ |
| level_736 | OK | 8 / 14 | 26 | 200 | 0/0 | coral 64/64, purple 57/57, yellow 48/45, sponge 1/1 | Won ✔ |
| level_737 | OK | 9 / 11 | 21 | 194 | 0/0 | purple 74/72, yellow 58/57, sponge 1/1, envelope 14/10 | Won ✔ |
| level_738 | OK | 11 / 14 | 23 | 145 | 0/0 | yellow 29/29, blue 44/43, coral 48/48, sponge 1/1 | Won ✔ |
| level_739 | OK | 8 / 12 | 24 | 193 | 0/0 | blue 51/51, coral 55/55, purple 33/31, sponge 1/1 | Won ✔ |
| level_740 | OK | 14 / 20 | 24 | 154 | 0/0 | coral 33/32, purple 59/59, yellow 41/41 | Won ✔ |
| level_741 | OK | 10 / 19 | 20 | 145 | 0/0 | purple 48/48, yellow 27/27 | Won ✔ |
| level_742 | OK | 8 / 16 | 24 | 160 | 0/0 | yellow 30/30, blue 46/42 | Won ✔ |
| level_743 | OK | 8 / 11 | 18 | 132 | 0/0 | blue 45/45, coral 41/37 | Won ✔ |
| level_744 | OK | 7 / 11 | 19 | 164 | 0/0 | coral 49/48, purple 25/22, yellow 38/31, sponge 1/1 | Won ✔ |
| level_745 | OK | 6 / 14 | 16 | 160 | 0/0 | purple 37/37, yellow 42/41, blue 53/47, sponge 1/1 | Won ✔ |
| level_746 | OK | 7 / 10 | 22 | 189 | 0/0 | yellow 35/35, blue 63/61, coral 63/60, envelope 2/2, sponge 1/1 | Won ✔ |
| level_747 | OK | 8 / 12 | 18 | 164 | 0/0 | blue 48/46, coral 41/41, purple 30/30, sponge 1/1 | Won ✔ |
| level_748 | OK | 14 / 16 | 26 | 154 | 0/0 | coral 53/53, purple 42/41, yellow 25/24, sponge 1/1 | Won ✔ |
| level_749 | OK | 1 / 1 | 3 | 20 | 0/0 | blue 13/12 | Won ✔ |
| level_750 | OK | 8 / 11 | 22 | 192 | 0/0 | yellow 45/44, blue 56/53, coral 44/44 | Won ✔ |
| level_751 | OK | 11 / 16 | 24 | 174 | 0/0 | blue 54/54, coral 31/31 | Won ✔ |
| level_752 | OK | 6 / 9 | 12 | 105 | 0/0 | coral 40/40, purple 35/34 | Won ✔ |
| level_753 | OK | 9 / 12 | 20 | 123 | 0/0 | purple 36/36, yellow 27/23 | Won ✔ |
| level_754 | OK | 7 / 10 | 18 | 167 | 0/0 | yellow 69/69, blue 42/41, coral 46/38, sponge 1/1 | Won ✔ |
| level_755 | OK | 13 / 21 | 26 | 199 | 0/0 | blue 39/38, coral 53/51, purple 58/58, sponge 1/1 | Won ✔ |
| level_756 | OK | 8 / 16 | 23 | 210 | 0/0 | coral 63/62, purple 54/54, yellow 24/23, sponge 1/1 | Won ✔ |
| level_757 | OK | 9 / 14 | 21 | 178 | 0/0 | purple 51/51, yellow 51/51, blue 32/31, sponge 1/1 | Won ✔ |
| level_758 | OK | 10 / 12 | 23 | 191 | 0/0 | yellow 49/49, blue 40/38, coral 44/33, sponge 1/1 | Won ✔ |
| level_759 | OK | 7 / 13 | 19 | 138 | 0/0 | blue 33/33, coral 46/46, purple 32/29, sponge 1/1 | Won ✔ |
| level_760 | OK | 10 / 14 | 22 | 177 | 0/0 | coral 50/48, purple 55/54, yellow 27/27 | Won ✔ |
| level_761 | OK | 8 / 15 | 20 | 149 | 0/0 | purple 38/38, yellow 38/38 | Won ✔ |
| level_762 | OK | 6 / 11 | 18 | 132 | 0/0 | yellow 53/53, blue 30/29 | Won ✔ |
| level_763 | OK | 6 / 9 | 18 | 139 | 0/0 | blue 43/41, coral 28/28 | Won ✔ |
| level_764 | OK | 8 / 10 | 19 | 156 | 0/0 | coral 41/41, purple 34/30, yellow 43/42, sponge 1/1 | Won ✔ |
| level_765 | OK | 6 / 9 | 18 | 146 | 0/0 | purple 30/28, yellow 31/31, blue 39/29, sponge 1/1 | Won ✔ |
| level_766 | OK | 7 / 11 | 17 | 175 | 0/0 | yellow 51/49, blue 48/46, coral 20/20, envelope 2/2, sponge 1/1 | Won ✔ |
| level_767 | OK | 7 / 12 | 20 | 175 | 0/0 | blue 49/49, coral 46/46, purple 43/37, sponge 1/1 | Won ✔ |
| level_768 | OK | 8 / 12 | 21 | 158 | 0/0 | coral 27/27, purple 59/59, yellow 43/43, sponge 1/1 | Won ✔ |
| level_769 | OK | 10 / 13 | 29 | 206 | 0/0 | purple 58/58, yellow 47/44, blue 52/47, sponge 1/1 | Won ✔ |
| level_770 | OK | 6 / 10 | 17 | 145 | 0/0 | blue 52/51, coral 34/34, envelope 3/3 | Won ✔ |
| level_771 | OK | 7 / 11 | 16 | 114 | 0/0 | blue 38/38, coral 32/32 | Won ✔ |
| level_772 | OK | 11 / 15 | 23 | 161 | 0/0 | coral 41/41, purple 35/34 | Won ✔ |
| level_773 | OK | 8 / 14 | 20 | 143 | 0/0 | purple 47/47, yellow 42/42 | Won ✔ |
| level_774 | OK | 6 / 10 | 22 | 157 | 0/0 | yellow 53/52, blue 38/38, coral 41/41, sponge 1/1 | Won ✔ |
| level_775 | OK | 6 / 14 | 20 | 215 | 0/0 | blue 48/47, coral 52/52, purple 63/62, sponge 1/1 | Won ✔ |
| level_776 | OK | 6 / 10 | 20 | 194 | 0/0 | coral 45/45, purple 69/68, yellow 51/45, sponge 1/1 | Won ✔ |
| level_777 | OK | 9 / 12 | 25 | 150 | 0/0 | purple 38/38, blue 53/50, sponge 1/1, envelope 15/10 | Won ✔ |
| level_778 | OK | 6 / 11 | 19 | 151 | 0/0 | yellow 47/44, blue 36/34, coral 41/34, sponge 1/1 | Won ✔ |
| level_779 | OK | 7 / 9 | 21 | 153 | 0/0 | blue 33/33, coral 40/37, purple 51/45, sponge 1/1 | Won ✔ |
| level_780 | OK | 8 / 13 | 22 | 202 | 0/0 | coral 48/48, purple 75/70, yellow 63/63 | Won ✔ |
| level_781 | OK | 11 / 20 | 21 | 155 | 0/0 | purple 22/22, yellow 69/68 | Won ✔ |
| level_782 | OK | 6 / 11 | 15 | 125 | 0/0 | yellow 38/38, blue 29/29 | Won ✔ |
| level_783 | OK | 12 / 18 | 24 | 170 | 0/0 | blue 37/37, coral 47/47 | Won ✔ |
| level_784 | OK | 5 / 8 | 17 | 163 | 0/0 | coral 34/34, purple 73/68, yellow 31/30, sponge 1/1 | Won ✔ |
| level_785 | OK | 9 / 14 | 22 | 195 | 0/0 | purple 58/58, yellow 52/52, blue 49/46, sponge 1/1 | Won ✔ |
| level_786 | OK | 6 / 11 | 18 | 158 | 0/0 | yellow 51/51, blue 45/43, coral 29/28, envelope 2/2, sponge 1/1 | Won ✔ |
| level_787 | OK | 9 / 11 | 17 | 166 | 0/0 | blue 34/33, coral 59/59, purple 43/35, sponge 1/1 | Won ✔ |
| level_788 | OK | 8 / 14 | 24 | 188 | 0/0 | coral 46/46, purple 63/62, yellow 21/20, sponge 1/1 | Won ✔ |
| level_789 | OK | 8 / 16 | 21 | 155 | 0/0 | purple 56/56, yellow 29/29, blue 39/36, sponge 1/1 | Won ✔ |
| level_790 | OK | 13 / 18 | 27 | 229 | 0/0 | yellow 46/46, blue 93/87, coral 39/36 | Won ✔ |
| level_791 | OK | 9 / 16 | 21 | 158 | 0/0 | blue 33/33, coral 36/36 | Won ✔ |
| level_792 | OK | 9 / 16 | 15 | 135 | 0/0 | coral 29/29, purple 43/43 | Won ✔ |
| level_793 | OK | 9 / 11 | 20 | 135 | 0/0 | purple 46/46, yellow 27/27 | Won ✔ |
| level_794 | OK | 8 / 12 | 21 | 172 | 0/0 | yellow 33/33, blue 31/30, coral 55/55, sponge 1/1 | Won ✔ |
| level_795 | OK | 9 / 19 | 23 | 167 | 0/0 | blue 26/26, coral 51/49, purple 42/40, sponge 1/1 | Won ✔ |
| level_796 | OK | 9 / 12 | 22 | 199 | 0/0 | coral 53/53, purple 64/63, yellow 33/32, sponge 1/1 | Won ✔ |
| level_797 | OK | 5 / 10 | 17 | 149 | 0/0 | purple 59/54, yellow 28/27, blue 34/34, sponge 1/1 | Won ✔ |
| level_798 | OK | 10 / 12 | 19 | 146 | 0/0 | yellow 51/50, blue 36/34, coral 28/22, sponge 1/1 | Won ✔ |
| level_799 | OK | 1 / 1 | 3 | 19 | 0/0 | coral 12/10 | Won ✔ |
| level_800 | OK | 9 / 15 | 18 | 100 | 0/0 | coral 30/29, purple 32/28, envelope 3/3 | Won ✔ |
| level_801 | OK | 9 / 13 | 22 | 155 | 0/0 | purple 42/42, yellow 55/54 | Won ✔ |
| level_802 | OK | 7 / 13 | 17 | 131 | 0/0 | yellow 37/37, blue 38/32 | Won ✔ |
| level_803 | OK | 8 / 11 | 19 | 145 | 0/0 | blue 41/41, coral 32/32 | Won ✔ |
| level_804 | OK | 10 / 18 | 22 | 152 | 0/0 | coral 38/37, purple 31/31, yellow 30/30 | Won ✔ |
| level_805 | OK | 9 / 12 | 28 | 176 | 0/0 | purple 37/37, yellow 48/48, blue 36/35, sponge 1/1 | Won ✔ |
| level_806 | OK | 9 / 15 | 28 | 200 | 0/0 | yellow 47/47, blue 39/37, coral 49/49, envelope 2/2 | Won ✔ |
| level_807 | OK | 7 / 11 | 24 | 139 | 0/0 | blue 25/25, coral 43/37, purple 35/27, envelope 2/2 | Won ✔ |
| level_808 | OK | 8 / 13 | 21 | 176 | 0/0 | coral 45/45, purple 42/41, yellow 47/47, sponge 1/1 | Won ✔ |
| level_809 | OK | 7 / 18 | 20 | 194 | 0/0 | purple 61/61, yellow 42/42, blue 41/41 | Won ✔ |
| level_810 | OK | 9 / 17 | 32 | 235 | 0/0 | yellow 67/67, coral 65/65, envelope 13/10 | Won ✔ |
| level_811 | OK | 5 / 8 | 16 | 132 | 0/0 | blue 49/49, coral 41/39 | Won ✔ |
| level_812 | OK | 9 / 14 | 23 | 153 | 0/0 | coral 50/49, purple 51/51 | Won ✔ |
| level_813 | OK | 8 / 10 | 19 | 140 | 0/0 | purple 44/44, yellow 43/40, sponge 1/1 | Won ✔ |
| level_814 | OK | 9 / 19 | 21 | 179 | 0/0 | yellow 61/61, blue 46/46, coral 51/50, envelope 2/2 | Won ✔ |
| level_815 | OK | 9 / 15 | 21 | 172 | 0/0 | blue 41/41, coral 35/35, purple 48/45, envelope 2/2 | Won ✔ |
| level_816 | OK | 9 / 11 | 23 | 164 | 0/0 | coral 54/53, purple 42/42, yellow 38/38, sponge 1/1 | Won ✔ |
| level_817 | OK | 10 / 12 | 27 | 178 | 0/0 | yellow 48/48, blue 39/39, envelope 8/7 | Won ✔ |
| level_818 | OK | 9 / 13 | 23 | 158 | 0/0 | yellow 45/43, blue 52/52, coral 41/36 | Won ✔ |
| level_819 | OK | 9 / 16 | 21 | 193 | 0/0 | blue 63/57, coral 71/67, purple 21/21 | Won ✔ |
| level_820 | OK | 12 / 18 | 27 | 221 | 0/0 | coral 58/58, purple 41/41, yellow 58/57 | Won ✔ |
| level_821 | OK | 10 / 16 | 25 | 154 | 0/0 | purple 67/67, yellow 28/28, sponge 1/1 | Won ✔ |
| level_822 | OK | 11 / 16 | 26 | 152 | 0/0 | yellow 31/31, blue 46/46, envelope 2/2 | Won ✔ |
| level_823 | OK | 7 / 11 | 17 | 125 | 0/0 | blue 36/36, coral 56/56, envelope 2/2 | Won ✔ |
| level_824 | OK | 6 / 12 | 18 | 139 | 0/0 | coral 37/37, purple 33/33, yellow 33/32, sponge 1/1 | Won ✔ |
| level_825 | OK | 7 / 17 | 23 | 170 | 0/0 | purple 50/48, yellow 44/43, blue 43/35 | Won ✔ |
| level_826 | OK | 8 / 10 | 21 | 185 | 0/0 | yellow 52/52, blue 42/42, coral 58/54 | Won ✔ |
| level_827 | OK | 8 / 12 | 18 | 188 | 0/0 | blue 59/59, coral 47/47, purple 46/44 | Won ✔ |
| level_828 | OK | 12 / 16 | 29 | 183 | 0/0 | coral 44/43, purple 53/47, yellow 49/37 | Won ✔ |
| level_829 | OK | 9 / 14 | 28 | 213 | 0/0 | purple 53/53, yellow 52/52, blue 48/45, sponge 1/1 | Won ✔ |
| level_830 | OK | 12 / 16 | 26 | 206 | 0/0 | yellow 31/31, blue 82/82, coral 43/43, envelope 2/2 | Won ✔ |
| level_831 | OK | 11 / 22 | 23 | 154 | 0/0 | blue 42/41, coral 49/49, envelope 2/2 | Won ✔ |
| level_832 | OK | 8 / 11 | 16 | 131 | 0/0 | coral 31/31, purple 40/40, sponge 1/1 | Won ✔ |
| level_833 | OK | 6 / 9 | 16 | 139 | 0/0 | purple 40/40, yellow 38/38 | Won ✔ |
| level_834 | OK | 8 / 14 | 24 | 185 | 0/0 | yellow 42/41, blue 58/58, coral 47/46 | Won ✔ |
| level_835 | OK | 10 / 18 | 24 | 171 | 0/0 | blue 28/28, coral 48/48, purple 34/34 | Won ✔ |
| level_836 | OK | 9 / 13 | 23 | 189 | 0/0 | coral 56/56, purple 51/51, yellow 39/39 | Won ✔ |
| level_837 | OK | 7 / 9 | 19 | 166 | 0/0 | purple 60/59, yellow 48/47, blue 28/28, sponge 1/1 | Won ✔ |
| level_838 | OK | 9 / 9 | 23 | 168 | 0/0 | yellow 23/22, blue 56/56, coral 56/53, envelope 2/2 | Won ✔ |
| level_839 | OK | 6 / 12 | 24 | 197 | 0/0 | blue 38/37, coral 49/49, purple 70/66, envelope 2/2 | Won ✔ |
| level_840 | OK | 9 / 14 | 23 | 177 | 0/0 | coral 34/34, purple 49/49, yellow 40/40, sponge 1/1 | Won ✔ |
| level_841 | OK | 8 / 13 | 20 | 168 | 0/0 | purple 44/44, yellow 61/61 | Won ✔ |
| level_842 | OK | 9 / 19 | 21 | 157 | 0/0 | yellow 41/40, blue 37/37 | Won ✔ |
| level_843 | OK | 7 / 11 | 20 | 118 | 0/0 | blue 25/25, coral 36/36 | Won ✔ |
| level_844 | OK | 9 / 13 | 26 | 210 | 0/0 | coral 56/56, purple 65/63, yellow 44/37 | Won ✔ |
| level_845 | OK | 6 / 12 | 20 | 171 | 0/0 | purple 38/38, yellow 34/33, blue 55/54, sponge 1/1 | Won ✔ |
| level_846 | OK | 10 / 21 | 25 | 189 | 0/0 | yellow 48/48, blue 30/30, coral 54/47, envelope 2/2 | Won ✔ |
| level_847 | OK | 6 / 10 | 22 | 203 | 0/0 | blue 49/49, coral 65/65, purple 41/39, envelope 2/2 | Won ✔ |
| level_848 | OK | 7 / 13 | 22 | 178 | 0/0 | coral 55/52, purple 39/34, yellow 50/50, sponge 1/1 | Won ✔ |
| level_849 | OK | 1 / 1 | 3 | 15 | 0/0 | coral 11/11 | Won ✔ |
| level_850 | OK | 7 / 13 | 23 | 204 | 0/0 | yellow 85/84, blue 46/46, coral 42/42 | Won ✔ |
| level_851 | OK | 6 / 11 | 16 | 124 | 0/0 | blue 32/32, coral 27/27 | Won ✔ |
| level_852 | OK | 11 / 14 | 22 | 147 | 0/0 | coral 46/46, purple 20/20 | Won ✔ |
| level_853 | OK | 8 / 11 | 22 | 121 | 0/0 | purple 36/36, yellow 22/22, sponge 1/1 | Won ✔ |
| level_854 | OK | 7 / 11 | 20 | 185 | 0/0 | yellow 46/46, blue 68/63, coral 44/44, envelope 2/2 | Won ✔ |
| level_855 | OK | 8 / 10 | 21 | 179 | 0/0 | blue 51/51, coral 44/39, purple 52/49, envelope 2/2 | Won ✔ |
| level_856 | OK | 7 / 11 | 22 | 177 | 0/0 | coral 28/28, purple 42/39, yellow 72/72, sponge 1/1 | Won ✔ |
| level_857 | OK | 9 / 13 | 22 | 183 | 0/0 | purple 62/61, yellow 52/51, envelope 9/8 | Won ✔ |
| level_858 | OK | 5 / 16 | 17 | 108 | 0/0 | yellow 44/44, blue 30/30, coral 18/18 | Won ✔ |
| level_859 | OK | 6 / 12 | 23 | 153 | 0/0 | blue 62/62, coral 39/39, purple 30/30 | Won ✔ |
| level_860 | OK | 22 / 26 | 37 | 208 | 0/0 | coral 44/43, purple 68/60, yellow 53/39 | Won ✔ |
| level_861 | OK | 6 / 10 | 15 | 112 | 0/0 | purple 26/23, yellow 40/39, sponge 1/1 | Won ✔ |
| level_862 | OK | 6 / 13 | 13 | 122 | 0/0 | yellow 56/56, blue 8/8, envelope 2/2 | Won ✔ |
| level_863 | OK | 8 / 15 | 18 | 151 | 0/0 | blue 63/61, coral 51/50, envelope 2/2 | Won ✔ |
| level_864 | OK | 8 / 12 | 22 | 169 | 0/0 | coral 40/39, purple 39/39, yellow 34/27, sponge 1/1 | Won ✔ |
| level_865 | OK | 8 / 14 | 23 | 198 | 0/0 | purple 59/58, yellow 40/39, blue 51/50 | Won ✔ |
| level_866 | OK | 7 / 11 | 23 | 184 | 0/0 | yellow 50/50, blue 75/72, coral 22/21 | Won ✔ |
| level_867 | OK | 6 / 13 | 19 | 148 | 0/0 | blue 52/52, coral 35/31, purple 32/31 | Won ✔ |
| level_868 | OK | 19 / 21 | 23 | 143 | 0/0 | coral 31/30, purple 43/42, yellow 36/36 | Won ✔ |
| level_869 | OK | 7 / 11 | 18 | 144 | 0/0 | purple 41/41, yellow 27/27, blue 34/31, sponge 1/1 | Won ✔ |
| level_870 | OK | 7 / 10 | 22 | 219 | 0/0 | yellow 80/78, blue 33/33, coral 41/41, envelope 2/2 | Won ✔ |
| level_871 | OK | 10 / 13 | 30 | 168 | 0/0 | blue 51/50, coral 40/40, envelope 2/2 | Won ✔ |
| level_872 | OK | 5 / 12 | 15 | 115 | 0/0 | coral 44/44, purple 30/26, sponge 1/1 | Won ✔ |
| level_873 | OK | 7 / 12 | 19 | 143 | 0/0 | purple 35/31, yellow 37/37 | Won ✔ |
| level_874 | OK | 7 / 17 | 20 | 212 | 0/0 | yellow 37/34, blue 61/57, coral 56/55 | Won ✔ |
| level_875 | OK | 6 / 9 | 21 | 157 | 0/0 | blue 57/53, coral 35/35, purple 28/26 | Won ✔ |
| level_876 | OK | 9 / 13 | 21 | 158 | 0/0 | coral 29/29, purple 45/45, yellow 48/47 | Won ✔ |
| level_877 | OK | 7 / 12 | 19 | 168 | 0/0 | purple 46/46, yellow 40/39, blue 36/36, sponge 1/1 | Won ✔ |
| level_878 | OK | 7 / 10 | 18 | 174 | 0/0 | yellow 41/41, blue 45/43, coral 29/29, envelope 2/2 | Won ✔ |
| level_879 | OK | 6 / 16 | 21 | 175 | 0/0 | blue 41/39, coral 45/42, purple 58/55, envelope 2/2 | Won ✔ |
| level_880 | OK | 8 / 14 | 22 | 235 | 0/0 | coral 67/66, purple 64/63, yellow 69/67, sponge 1/1 | Won ✔ |
| level_881 | OK | 11 / 15 | 23 | 168 | 0/0 | purple 48/48, yellow 43/41 | Won ✔ |
| level_882 | OK | 7 / 14 | 18 | 132 | 0/0 | yellow 42/42, blue 32/32 | Won ✔ |
| level_883 | OK | 6 / 12 | 15 | 166 | 0/0 | blue 36/34, coral 45/43 | Won ✔ |
| level_884 | OK | 10 / 14 | 26 | 182 | 0/0 | coral 44/44, purple 63/63, yellow 42/42 | Won ✔ |
| level_885 | OK | 9 / 13 | 22 | 165 | 0/0 | purple 45/43, yellow 45/43, blue 49/48, sponge 1/1 | Won ✔ |
| level_886 | OK | 9 / 15 | 20 | 173 | 0/0 | yellow 61/61, blue 42/40, coral 25/21, envelope 2/2 | Won ✔ |
| level_887 | OK | 6 / 12 | 14 | 146 | 0/0 | blue 55/55, coral 37/37, purple 43/41, envelope 2/2 | Won ✔ |
| level_888 | OK | 10 / 16 | 23 | 147 | 0/0 | coral 28/28, purple 39/38, yellow 46/35, sponge 1/1 | Won ✔ |
| level_889 | OK | 7 / 13 | 21 | 189 | 0/0 | purple 48/46, yellow 59/59, blue 46/46 | Won ✔ |
| level_890 | OK | 13 / 16 | 30 | 217 | 0/0 | yellow 46/46, blue 49/49, envelope 27/10 | Won ✔ |
| level_891 | OK | 12 / 17 | 20 | 139 | 0/0 | blue 63/63, coral 25/25 | Won ✔ |
| level_892 | OK | 8 / 15 | 19 | 127 | 0/0 | coral 41/39, purple 29/29 | Won ✔ |
| level_893 | OK | 7 / 10 | 17 | 133 | 0/0 | purple 43/43, yellow 31/31, sponge 1/1 | Won ✔ |
| level_894 | OK | 12 / 18 | 27 | 171 | 0/0 | yellow 50/50, blue 54/54, coral 41/35, envelope 2/2 | Won ✔ |
| level_895 | OK | 8 / 10 | 23 | 180 | 0/0 | blue 48/48, coral 19/19, purple 36/36, envelope 2/2 | Won ✔ |
| level_896 | OK | 9 / 19 | 25 | 207 | 0/0 | coral 47/47, purple 66/62, yellow 66/57, sponge 1/1 | Won ✔ |
| level_897 | OK | 4 / 10 | 13 | 118 | 0/0 | purple 36/36, yellow 32/29, envelope 10/10 | Won ✔ |
| level_898 | OK | 7 / 14 | 23 | 150 | 0/0 | yellow 39/39, blue 44/44, coral 27/27 | Won ✔ |
| level_899 | OK | 1 / 1 | 3 | 13 | 0/0 | coral 9/9 | Won ✔ |
| level_900 | OK | 7 / 12 | 22 | 215 | 0/0 | coral 47/47, purple 48/48, yellow 55/51 | Won ✔ |
| level_901 | OK | 6 / 11 | 17 | 172 | 0/0 | purple 54/54, yellow 52/52, sponge 1/1 | Won ✔ |
| level_902 | OK | 11 / 14 | 19 | 144 | 0/0 | yellow 33/33, blue 49/48, envelope 2/2 | Won ✔ |
| level_903 | OK | 7 / 11 | 18 | 101 | 0/0 | blue 29/29, coral 29/29, envelope 2/2 | Won ✔ |
| level_904 | OK | 6 / 9 | 17 | 147 | 0/0 | coral 45/44, purple 35/33, yellow 37/30, sponge 1/1 | Won ✔ |
| level_905 | OK | 8 / 14 | 23 | 200 | 0/0 | purple 63/61, yellow 42/40, blue 55/54 | Won ✔ |
| level_906 | OK | 6 / 10 | 16 | 148 | 0/0 | yellow 47/46, blue 48/48, coral 30/27 | Won ✔ |
| level_907 | OK | 6 / 12 | 17 | 180 | 0/0 | blue 53/53, coral 38/38, purple 25/25 | Won ✔ |
| level_908 | OK | 20 / 24 | 27 | 154 | 0/0 | coral 30/27, purple 40/37, yellow 52/51 | Won ✔ |
| level_909 | OK | 8 / 12 | 23 | 171 | 0/0 | purple 47/46, yellow 46/46, blue 32/31, sponge 1/1 | Won ✔ |
| level_910 | OK | 7 / 13 | 24 | 228 | 0/0 | yellow 66/66, blue 40/39, coral 66/62, envelope 2/2 | Won ✔ |
| level_911 | OK | 6 / 13 | 17 | 151 | 0/0 | blue 45/44, coral 32/31, envelope 2/2 | Won ✔ |
| level_912 | OK | 6 / 12 | 20 | 108 | 0/0 | coral 34/34, purple 35/34, sponge 1/1 | Won ✔ |
| level_913 | OK | 6 / 10 | 18 | 157 | 0/0 | purple 49/49, yellow 37/37 | Won ✔ |
| level_914 | OK | 5 / 8 | 19 | 180 | 0/0 | yellow 51/51, blue 50/46, coral 53/46 | Won ✔ |
| level_915 | OK | 10 / 19 | 24 | 188 | 0/0 | blue 59/59, coral 35/35, purple 47/44 | Won ✔ |
| level_916 | OK | 11 / 22 | 26 | 218 | 0/0 | coral 62/62, purple 56/56, yellow 56/56 | Won ✔ |
| level_917 | OK | 6 / 8 | 22 | 187 | 0/0 | purple 43/42, yellow 49/49, blue 49/45, sponge 1/1 | Won ✔ |
| level_918 | OK | 8 / 16 | 26 | 186 | 0/0 | yellow 49/49, blue 51/51, coral 38/38, envelope 2/2 | Won ✔ |
| level_919 | OK | 7 / 11 | 24 | 202 | 0/0 | blue 60/60, coral 38/38, purple 49/49, envelope 2/2 | Won ✔ |
| level_920 | OK | 9 / 14 | 24 | 153 | 0/0 | coral 47/47, purple 35/30, yellow 32/29, sponge 1/1 | Won ✔ |
| level_921 | OK | 8 / 13 | 17 | 111 | 0/0 | purple 46/46, yellow 34/34 | Won ✔ |
| level_922 | OK | 9 / 19 | 25 | 165 | 0/0 | yellow 41/41, blue 40/40 | Won ✔ |
| level_923 | OK | 10 / 20 | 20 | 155 | 0/0 | blue 44/44, coral 27/26 | Won ✔ |
| level_924 | OK | 8 / 12 | 25 | 197 | 0/0 | coral 61/61, purple 56/56, yellow 45/39 | Won ✔ |
| level_925 | OK | 7 / 10 | 17 | 156 | 0/0 | purple 67/66, yellow 41/41, blue 18/18, sponge 1/1 | Won ✔ |
| level_926 | OK | 7 / 15 | 23 | 213 | 0/0 | yellow 74/74, blue 53/53, coral 48/47, envelope 2/2 | Won ✔ |
| level_927 | OK | 7 / 10 | 23 | 192 | 0/0 | blue 60/57, coral 42/41, purple 48/48, envelope 2/2 | Won ✔ |
| level_928 | OK | 10 / 15 | 21 | 192 | 0/0 | coral 47/47, purple 45/45, yellow 63/63, sponge 1/1 | Won ✔ |
| level_929 | OK | 9 / 15 | 22 | 197 | 0/0 | purple 47/47, yellow 69/66, blue 34/31 | Won ✔ |
| level_930 | OK | 9 / 15 | 23 | 195 | 0/0 | yellow 68/67, blue 28/27, coral 55/53 | Won ✔ |
| level_931 | OK | 6 / 9 | 17 | 146 | 0/0 | blue 39/39, coral 39/35 | Won ✔ |
| level_932 | OK | 9 / 15 | 22 | 140 | 0/0 | coral 34/34, purple 28/28 | Won ✔ |
| level_933 | OK | 8 / 15 | 19 | 141 | 0/0 | purple 51/51, yellow 42/38, sponge 1/1 | Won ✔ |
| level_934 | OK | 7 / 10 | 22 | 192 | 0/0 | yellow 56/56, blue 42/40, coral 48/46, envelope 2/2 | Won ✔ |
| level_935 | OK | 7 / 13 | 23 | 206 | 0/0 | blue 58/57, coral 53/52, purple 40/40, envelope 2/2 | Won ✔ |
| level_936 | OK | 7 / 11 | 26 | 172 | 0/0 | coral 38/38, purple 62/60, yellow 30/30, sponge 1/1 | Won ✔ |
| level_937 | OK | 10 / 15 | 23 | 169 | 0/0 | purple 66/66, yellow 40/40, envelope 11/10 | Won ✔ |
| level_938 | OK | 7 / 13 | 17 | 130 | 0/0 | yellow 42/42, blue 32/32, coral 29/29 | Won ✔ |
| level_939 | OK | 9 / 18 | 22 | 166 | 0/0 | blue 63/63, coral 48/46, purple 25/23 | Won ✔ |
| level_940 | OK | 14 / 18 | 24 | 159 | 0/0 | coral 40/39, purple 48/48, yellow 29/29 | Won ✔ |
| level_941 | OK | 5 / 10 | 10 | 101 | 0/0 | purple 44/43, yellow 25/23, sponge 1/1 | Won ✔ |
| level_942 | OK | 8 / 11 | 19 | 109 | 0/0 | yellow 32/32, blue 22/22, envelope 2/2 | Won ✔ |
| level_943 | OK | 10 / 14 | 23 | 168 | 0/0 | blue 60/59, coral 60/60, envelope 2/2 | Won ✔ |
| level_944 | OK | 7 / 11 | 21 | 173 | 0/0 | coral 44/43, purple 30/30, yellow 38/38, sponge 1/1 | Won ✔ |
| level_945 | OK | 11 / 15 | 29 | 155 | 0/0 | purple 37/37, yellow 45/44, blue 35/35 | Won ✔ |
| level_946 | OK | 8 / 12 | 27 | 174 | 0/0 | yellow 36/36, blue 48/47, coral 40/34 | Won ✔ |
| level_947 | OK | 9 / 17 | 22 | 202 | 0/0 | blue 57/57, coral 46/44, purple 56/55 | Won ✔ |
| level_948 | OK | 9 / 14 | 22 | 192 | 0/0 | coral 63/62, purple 39/39, yellow 35/35 | Won ✔ |
| level_949 | OK | 1 / 1 | 3 | 29 | 0/0 | yellow 19/13 | Won ✔ |
| level_950 | OK | 11 / 15 | 21 | 205 | 0/0 | yellow 66/65, blue 51/50, coral 48/47, envelope 2/2 | Won ✔ |
| level_951 | OK | 6 / 15 | 14 | 100 | 0/0 | blue 11/11, coral 40/38, envelope 2/2 | Won ✔ |
| level_952 | OK | 6 / 9 | 18 | 154 | 0/0 | coral 45/44, purple 50/48, sponge 1/1 | Won ✔ |
| level_953 | OK | 6 / 9 | 20 | 152 | 0/0 | purple 35/35, yellow 45/45 | Won ✔ |
| level_954 | OK | 6 / 9 | 18 | 152 | 0/0 | yellow 23/23, blue 37/37, coral 55/53 | Won ✔ |
| level_955 | OK | 7 / 15 | 22 | 178 | 0/0 | blue 35/35, coral 68/66, purple 53/53 | Won ✔ |
| level_956 | OK | 8 / 15 | 24 | 196 | 0/0 | coral 52/52, purple 51/51, yellow 53/52 | Won ✔ |
| level_957 | OK | 5 / 10 | 18 | 158 | 0/0 | purple 36/36, yellow 52/48, blue 34/29, sponge 1/1 | Won ✔ |
| level_958 | OK | 8 / 14 | 22 | 174 | 0/0 | yellow 32/32, blue 46/46, coral 40/37, envelope 2/2 | Won ✔ |
| level_959 | OK | 10 / 14 | 24 | 152 | 0/0 | blue 65/65, coral 38/38, purple 34/34, envelope 2/2 | Won ✔ |
| level_960 | OK | 11 / 14 | 25 | 176 | 0/0 | coral 55/49, purple 25/25, yellow 45/33, sponge 1/1 | Won ✔ |
| level_961 | OK | 5 / 9 | 18 | 168 | 0/0 | purple 71/71, yellow 36/36 | Won ✔ |
| level_962 | OK | 6 / 11 | 16 | 136 | 0/0 | yellow 35/35, blue 32/29 | Won ✔ |
| level_963 | OK | 6 / 9 | 18 | 122 | 0/0 | blue 48/47, coral 37/37 | Won ✔ |
| level_964 | OK | 8 / 14 | 21 | 216 | 0/0 | coral 50/50, purple 70/69, yellow 48/46 | Won ✔ |
| level_965 | OK | 9 / 12 | 22 | 180 | 0/0 | purple 47/46, yellow 46/46, blue 40/35, sponge 1/1 | Won ✔ |
| level_966 | OK | 8 / 13 | 23 | 123 | 0/0 | yellow 28/28, blue 43/43, coral 28/25, envelope 2/2 | Won ✔ |
| level_967 | OK | 8 / 12 | 24 | 192 | 0/0 | blue 43/43, coral 54/52, purple 50/50, envelope 2/2 | Won ✔ |
| level_968 | OK | 7 / 11 | 24 | 195 | 0/0 | coral 79/78, purple 51/51, yellow 34/34, sponge 1/1 | Won ✔ |
| level_969 | OK | 10 / 14 | 24 | 139 | 0/0 | purple 38/38, yellow 36/36, blue 40/37 | Won ✔ |
| level_970 | OK | 8 / 12 | 30 | 183 | 0/0 | blue 67/67, coral 41/40, envelope 16/10 | Won ✔ |
| level_971 | OK | 7 / 12 | 16 | 119 | 0/0 | blue 41/41, coral 22/22 | Won ✔ |
| level_972 | OK | 9 / 16 | 22 | 154 | 0/0 | coral 38/38, purple 30/30 | Won ✔ |
| level_973 | OK | 8 / 13 | 22 | 135 | 0/0 | purple 45/45, yellow 26/26, sponge 1/1 | Won ✔ |
| level_974 | OK | 7 / 13 | 22 | 167 | 0/0 | yellow 65/64, blue 42/42, coral 36/36, envelope 2/2 | Won ✔ |
| level_975 | OK | 7 / 10 | 19 | 168 | 0/0 | blue 48/48, coral 36/36, purple 31/30, envelope 2/2 | Won ✔ |
| level_976 | OK | 9 / 17 | 22 | 147 | 0/0 | coral 32/32, purple 46/45, yellow 51/46, sponge 1/1 | Won ✔ |
| level_977 | OK | 8 / 14 | 21 | 193 | 0/0 | purple 55/55, yellow 48/45, blue 47/47 | Won ✔ |
| level_978 | OK | 8 / 17 | 23 | 170 | 0/0 | yellow 61/61, blue 41/41, coral 30/30 | Won ✔ |
| level_979 | OK | 6 / 8 | 19 | 146 | 0/0 | blue 38/38, coral 52/48, purple 33/33 | Won ✔ |
| level_980 | OK | 10 / 16 | 28 | 248 | 0/0 | coral 78/75, purple 38/38, yellow 59/59 | Won ✔ |
| level_981 | OK | 5 / 11 | 18 | 141 | 0/0 | purple 53/51, yellow 34/32, sponge 1/1 | Won ✔ |
| level_982 | OK | 6 / 12 | 18 | 164 | 0/0 | yellow 44/43, blue 42/39, envelope 2/2 | Won ✔ |
| level_983 | OK | 10 / 12 | 18 | 132 | 0/0 | blue 45/45, coral 41/41, envelope 2/2 | Won ✔ |
| level_984 | OK | 7 / 11 | 24 | 192 | 0/0 | coral 41/41, purple 52/51, yellow 43/42, sponge 1/1 | Won ✔ |
| level_985 | OK | 6 / 11 | 17 | 143 | 0/0 | purple 43/43, yellow 45/44, blue 45/44 | Won ✔ |
| level_986 | OK | 5 / 9 | 18 | 168 | 0/0 | yellow 42/41, blue 37/35, coral 52/45 | Won ✔ |
| level_987 | OK | 9 / 12 | 20 | 154 | 0/0 | blue 44/44, coral 49/46, purple 30/30 | Won ✔ |
| level_988 | OK | 17 / 18 | 27 | 159 | 0/0 | coral 43/43, purple 49/43, yellow 25/25 | Won ✔ |
| level_989 | OK | 9 / 15 | 24 | 175 | 0/0 | purple 34/33, yellow 54/53, blue 55/55, sponge 1/1 | Won ✔ |
| level_990 | OK | 7 / 12 | 27 | 221 | 0/0 | yellow 79/79, blue 51/51, coral 53/50, envelope 2/2 | Won ✔ |
| level_991 | OK | 7 / 12 | 21 | 171 | 0/0 | blue 48/47, coral 53/52, envelope 2/2 | Won ✔ |
| level_992 | OK | 6 / 9 | 14 | 122 | 0/0 | coral 43/43, purple 20/20, sponge 1/1 | Won ✔ |
| level_993 | OK | 7 / 11 | 17 | 141 | 0/0 | purple 47/47, yellow 38/38 | Won ✔ |
| level_994 | OK | 8 / 12 | 20 | 180 | 0/0 | yellow 46/45, blue 43/43, coral 38/38 | Won ✔ |
| level_995 | OK | 7 / 11 | 19 | 161 | 0/0 | blue 47/47, coral 37/36, purple 39/31 | Won ✔ |
| level_996 | OK | 9 / 12 | 21 | 191 | 0/0 | coral 44/44, purple 46/46, yellow 48/47 | Won ✔ |
| level_997 | OK | 8 / 11 | 21 | 164 | 0/0 | purple 39/38, yellow 65/63, blue 38/37, sponge 1/1 | Won ✔ |
| level_998 | OK | 9 / 16 | 29 | 200 | 0/0 | yellow 57/55, blue 40/40, coral 46/45, envelope 2/2 | Won ✔ |
| level_999 | OK | 1 / 1 | 3 | 19 | 0/0 | coral 13/11 | Won ✔ |
