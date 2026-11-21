FixClippingForInteraction:
    LDA $08
    PHA
    LDA $01
    STA !HitmanHB2Top
    PLA
    STA !HitmanHB2Left+1

    ;$00 = X (16 bits)
    ;$08 = Y (16 bits)

    LDA $03
    STA $0C                         

    STZ !HitmanHB2Right+1
    STZ !HitmanHB2Bottom+1

    REP #$21
    LDA !HitmanHB2Left
    ADC #$0100
    STA !HitmanHB2Left
    CLC
    ADC $02
    STA !HitmanHB2Right

    LDA !HitmanHB2Top
    ADC #$0100
    STA !HitmanHB2Top
    CLC
    ADC $0C
    STA !HitmanHB2Bottom
    SEP #$20

RTL                       ; Return 