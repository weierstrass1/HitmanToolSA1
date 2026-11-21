;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; $B829 - vertical mario/sprite position check - shared
; Y = 1 if mario below sprite??
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

                    ;org $03B829 
SubVertPos:
    LDY #$00
    LDA $96
    SEC
    SBC !SpriteYLow,x
    STA $0F
    LDA $97
    SBC !SpriteYHigh,x
    BPL +
    INY
+
    RTL



