macro HitmanCapeClippingBox(Left, Right, Top, Bottom)
    REP #$21
    LDA $13E9|!addr
    ADC #$00FE
    STA <Left>
    CLC
    ADC #$0014
    STA <Right>
    SEP #$20

    REP #$21
    LDA $13EB|!addr
    ADC #$0100
    STA <Top>
    CLC
    ADC #$0010
    STA <Bottom>
    SEP #$20
endmacro

CapeClipping:
    %HitmanCapeClippingBox(!HitmanHB2Left, !HitmanHB2Right, !HitmanHB2Top, !HitmanHB2Bottom)
    RTL
