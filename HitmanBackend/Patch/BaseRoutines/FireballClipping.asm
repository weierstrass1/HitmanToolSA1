macro HitmanFireballClippingBox(Left, Right, Top, Bottom)
    LDA !ExtendedXHigh,y
    XBA
    LDA !ExtendedXLow,y
    REP #$21
    ADC #$00FE
    STA <Left>
    CLC
    ADC #$000C
    STA <Right>
    SEP #$20

    LDA !ExtendedYHigh,y
    XBA
    LDA !ExtendedYLow,y
    REP #$21
    ADC #$00FC
    STA <Top>
    CLC
    ADC #$0013
    STA <Bottom>
    SEP #$20
endmacro

FireballClipping:
    %HitmanFireballClippingBox(!HitmanHB2Left, !HitmanHB2Right, !HitmanHB2Top, !HitmanHB2Bottom)
    RTL
