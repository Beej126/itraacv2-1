<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" omit-xml-declaration="yes" indent="yes"/>

  <!-- this is pretty cool but it requires the xml doc to be presented to the transform object as a reader 
         might switch to that if becomes necessary -->
  <!--xsl:strip-space elements="*"/-->

  <!--html>
    <header>
      <script src="http://ajax.googleapis.com/ajax/libs/jquery/1.7.2/jquery.min.js"></script>
    </header>
    <body style="font-family: consolas">apply templates would go here</body></html-->

  <xsl:template match="/">
    <xsl:apply-templates select="node()" />
  </xsl:template>

  <xsl:template match="node()">
    <xsl:param name="parentIdx"/>

    <xsl:variable name="apos">'</xsl:variable>

    <table border="1" style="margin-left:20px; border-collapse:collapse;" cellpadding="3">
      <tr>
        <xsl:if test="count(*[1]/*) &gt; 0">
          <th></th>
        </xsl:if>

        <xsl:for-each select="*[1]/@*">
          <th bgcolor="#9acd32">
            <xsl:value-of select="name()" />
          </th>
        </xsl:for-each>
      </tr>

      <xsl:for-each select="*">
        <tr>
          <xsl:if test="count(*) &gt; 0">
            <th style="cursor: hand">
              <xsl:attribute name="onclick">
                <xsl:value-of select='concat("var vis = $(", $apos, "#", generate-id(), $apos, ").toggle().is(", $apos, ":visible", $apos, "); $(this).html(vis?", $apos, "-", $apos, ":", $apos, "+", $apos, ")")' />
              </xsl:attribute>
              +
            </th>
          </xsl:if>
          <xsl:for-each select="@*">
            <td>
              <xsl:value-of select="." />
            </td>
          </xsl:for-each>
        </tr>

        <tr style="display: none">
          <xsl:attribute name="id">
            <xsl:value-of select="generate-id()" />
          </xsl:attribute>
          <td>
            <xsl:attribute name="colspan">
              <xsl:value-of select="count(@*) + 1" />
            </xsl:attribute>

            <xsl:if test="count(*) &gt; 0">
              <xsl:apply-templates select="." />
            </xsl:if>

          </td>
        </tr>
      </xsl:for-each>

    </table>

  </xsl:template>

</xsl:stylesheet>

