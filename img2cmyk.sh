#!/bin/bash
# Script que sirve para convertir una imagen a formato CMYK con el perfil indicado
# ver. 20220812
# jEsuSdA 8)

# Requiere: IMAGEMAGICK

# Uso: img2cmyk.sh <image> [tif]
echo "Uso: img2cmyk.sh <image> [tif]"

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



PERFILRGB="srgb-color-space-profile.icm"
PERFILCMYK="psouncoated_v3_fogra52.icc"
COLORSPACE="-colorspace CMYK"
COLORSPACE=""
IMAGE="$1"

# Esto lo añado para usar como directorio temporal uno distinto a /temp
# Dado que el IMAGEMAGICK se suele quedar sin memoria y espacio cuando trabaja 
# con imágenes grandes.
#
# CÁMBIALÓ O COMÉNTALO SI TIENES PROBLEMAS:
export MAGICK_TMPDIR=/tmp




ORIGEN="$IMAGE"
cambiaext "$IMAGE"
DESTINO=$namefich"CMYK.jpg"
MESSAGE="Convirtiendo imagen $IMAGE a JPG CMYK..."
PARAM=""

if [ "$2" == "tif" ]
then

PARAM="-compress zip"
DESTINO=$namefich"CMYK.tif"
MESSAGE="Convirtiendo imagen $IMAGE a TIFF CMYK..."

fi



# env MAGICK_DISK_LIMIT=42GiB MAGICK_AREA_LIMIT=420MP   convert "$IMAGE"  -alpha off  -intent perceptual -black-point-compensation -profile "$PERFIL" "$DESTINO"


# Usaremos los parametros de densidad y resolución para definir el tamaño de impresión
# DENSITY = 118.11 pixelspercentimeter = 300 pixelsperinch

DENSITY="-density 300 -units pixelsperinch" 
DENSITY="-density 118.11 -units pixelspercentimeter"

# Si omitimos "-density", mantendremos la resolución del archivo original
DENSITY="-units pixelspercentimeter"



echo -n "$MESSAGE"

#convert "$IMAGE"  $COLORSPACE -profile "$PERFILRGB" -profile "$PERFILCMYK" -alpha off -intent perceptual -black-point-compensation "$DESTINO"


convert "$IMAGE"  -profile "$PERFILRGB" -alpha off $DENSITY -intent Relative -black-point-compensation -profile "$PERFILCMYK"  $PARAM "$DESTINO"


echo "..OK!"

exit

# https://imagousa.com/index.php/download_file/view/799/935/
# https://www.imagemagick.org/discourse-server/viewtopic.php?t=20674
# http://www.imagemagick.org/Usage/formats/#profiles
