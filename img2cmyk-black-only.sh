#!/bin/bash
# Script que sirve para convertir una imagen a escala de grises CMYK usando sólo el canal K
# ver. 20220812
# jEsuSdA 8)

# Requiere: IMAGEMAGICK

# Uso: img2cmyk-black-only.sh <image>


## IMPORTANTE ##

# Dependiendo de la imagen, es posible que IMAGEMAGICK se quede sin recursos para trabajar.
# Entonces lo que hay que hacer es cambiar la configuración que hay en 

# /etc/ImageMagick-6/policy.xml 

# para aumentar los valores de las variables de :

#   <policy domain="resource" name="memory" value="256MiB"/>
#   <policy domain="resource" name="map" value="512MiB"/>
#   <policy domain="resource" name="width" value="16KP"/>
#   <policy domain="resource" name="height" value="16KP"/>
#   <policy domain="resource" name="area" value="128MB"/>
#   <policy domain="resource" name="disk" value="1GiB"/>

# a:

#   <policy domain="resource" name="memory" value="6GiB"/>
#   <policy domain="resource" name="map" value="6GiB"/>
#   <policy domain="resource" name="width" value="64MP"/>
#   <policy domain="resource" name="height" value="64MP"/>
#   <policy domain="resource" name="area" value="6GiMB"/>
#   <policy domain="resource" name="disk" value="10GiB"/>


# Esta función recibe en $1 un nombre de fichero
# Devuelve en namefich ese mismo nombre pero sin extension.

function cambiaext {
    str=$1
    ext=`echo ${str:(-5)} | cut -d . -f 2`
    len_ext=${#ext}
    len_cad=${#str}
    titulo=$[len_cad-len_ext]
    namefich=${str:0:($titulo)}
}




IMAGE="$1"

# Esto lo añado para usar como directorio temporal uno distinto a /temp
# Dado que el IMAGEMAGICK se suele quedar sin memoria y espacio cuando trabaja 
# con imágenes grandes.
#
# CÁMBIALÓ O COMÉNTALO SI TIENES PROBLEMAS:
export MAGICK_TMPDIR=/tmp




ORIGEN="$IMAGE"
cambiaext "$IMAGE"
PARAM="-compress zip"
DESTINO=$namefich"CMYK-Key-only.tif"
MESSAGE="Convirtiendo imagen $IMAGE a TIFF CMYK, solo K..."




# Usaremos los parametros de densidad y resolución para definir el tamaño de impresión
# DENSITY = 118.11 pixelspercentimeter = 300 pixelsperinch

DENSITY="-density 300 -units pixelsperinch" 
DENSITY="-density 118.11 -units pixelspercentimeter"

# Si omitimos "-density", mantendremos la resolución del archivo original
DENSITY="-units pixelspercentimeter"


echo -n "$MESSAGE"

# Convertimos a escala de grises:
convert "$IMAGE" -alpha off $DENSITY -set colorspace Gray -separate -average ".tmp-img.png"

# Convertimos a CMYK dejando sólo el canal K con datos:
convert ".tmp-img.png" -alpha off $DENSITY -colorspace cmy -colorspace cmyk "$DESTINO"

# Eliminamos la imagen temporal
rm -rf ".tmp-img.png"


# TEST DE SEPARACIÓN DE CANALES (descomentar si se desean hacer comprobaciones):
# convert "$DESTINO" -colorspace cmyk -channel c         -separate channel_c.png
# convert "$DESTINO" -colorspace cmyk -channel m         -separate channel_m.png
# convert "$DESTINO" -colorspace cmyk -channel y         -separate channel_y.png
# convert "$DESTINO" -colorspace cmyk -channel k -negate -separate channel_k.png


echo "..OK!"

exit





