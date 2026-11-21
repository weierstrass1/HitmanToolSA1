YoshiFireballClipping:

    LDA !ExtendedXHigh,y
    XBA
    LDA !ExtendedXLow,y
    REP #$21
    ADC #$00FE
    STA $00
    CLC
    ADC #$000C
    STA $02
    SEP #$20

    LDA !ExtendedYHigh,y
    XBA
    LDA !ExtendedYLow,y
    REP #$21
    ADC #$00FC
    STA $08
    CLC
    ADC #$0013
    STA $0C
    SEP #$20
RTL
