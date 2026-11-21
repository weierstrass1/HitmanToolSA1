; Collect overrides before emitting the contiguous ROM tables.
macro SetExtendedHitboxSystem(id, mode)
    assert <id> >= 0 && <id> <= $FF, "Invalid extended sprite number"
    assert <mode> >= 0 && <mode> <= 4, "Invalid extended hitbox system"
    !extendedConfigID #= <id>
    !{ExtendedMode_!extendedConfigID} #= <mode>
endmacro

macro SetExtendedHitbox(id, xOffset, yOffset, width, height)
    assert <id> >= 0 && <id> <= $FF, "Invalid extended sprite number"
    assert <xOffset> >= -$8000 && <xOffset> <= $FFFF, "Invalid extended X offset"
    assert <yOffset> >= -$8000 && <yOffset> <= $FFFF, "Invalid extended Y offset"
    assert <width> >= 0 && <width> <= $FFFF, "Invalid extended width"
    assert <height> >= 0 && <height> <= $FFFF, "Invalid extended height"
    !extendedConfigID #= <id>
    !{ExtendedX_!extendedConfigID} #= (<xOffset>)&$FFFF
    !{ExtendedY_!extendedConfigID} #= (<yOffset>)&$FFFF
    !{ExtendedW_!extendedConfigID} #= <width>
    !{ExtendedH_!extendedConfigID} #= <height>
endmacro

macro SetExtendedFireballEvent(id, event)
    assert <id> >= 0 && <id> <= $FF, "Invalid extended sprite number"
    assert <event> == 0 || <event> == !HitmanEventFireball || <event> == !HitmanEventYoshiFireball, "Invalid extended fireball event"
    !extendedConfigID #= <id>
    !{ExtendedEvent_!extendedConfigID} #= <event>
endmacro

incsrc "ExtendedSpriteTablesConfig.asm"

macro EmitExtendedTable(field, directive, default)
    !extendedEntry = 0
    while !extendedEntry < 256
        if defined("<field>_!extendedEntry")
            <directive> !{<field>_!extendedEntry}
        else
            <directive> <default>
        endif
        !extendedEntry #= !extendedEntry+1
    endwhile
endmacro

ExtendedHitboxSystem:
    %EmitExtendedTable(ExtendedMode, db, $00)
ExtendedFireballEvent:
    %EmitExtendedTable(ExtendedEvent, db, $00)
ExtendedXOffset:
    %EmitExtendedTable(ExtendedX, dw, $FFFE)
ExtendedYOffset:
    %EmitExtendedTable(ExtendedY, dw, $FFFC)
ExtendedWidth:
    %EmitExtendedTable(ExtendedW, dw, $000C)
ExtendedHeight:
    %EmitExtendedTable(ExtendedH, dw, $0013)
