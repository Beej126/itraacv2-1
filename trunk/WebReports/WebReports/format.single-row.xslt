<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" omit-xml-declaration="yes" indent="yes"/>

  <xsl:variable name="IsNumeric" select="'0123456789,.'"/>

  <xsl:template match="NewDataSet/Table">
    <table border="1" style="border-collapse:collapse;" cellpadding="3">
        <xsl:for-each select="*">
          <tr>
            <td bgcolor="#9acd32" style="color: black">
              <xsl:value-of select="local-name()"/>
            </td>
            <td>
              <xsl:if test="string-length(translate(., $IsNumeric, '')) = 0">
                <xsl:attribute name="align">right</xsl:attribute>
              </xsl:if>
              <xsl:value-of select="."/>
            </td>
          </tr>
        </xsl:for-each>
    </table>
    <div style="margin-top: 10px">(end of results)</div>
  </xsl:template>
  
</xsl:stylesheet>

