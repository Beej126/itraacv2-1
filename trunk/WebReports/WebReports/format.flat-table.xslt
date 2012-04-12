<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:variable name="IsNumeric" select="'0123456789,.'"/>

  <xsl:template match="/">
    <div style="margin-bottom: 5px">Records: 
      <xsl:value-of select="count(NewDataSet/Table)"/>
    </div>
    <table border="1" style="border-collapse:collapse;" cellpadding="3">
      <tr bgcolor="#9acd32">
        <xsl:for-each select="NewDataSet/Table[1]/*">
          <th style="color: black">
            <xsl:value-of select="local-name()"/>
          </th>
        </xsl:for-each>
      </tr>
      <xsl:for-each select="NewDataSet/Table">
      <tr>
        <xsl:for-each select="*">
          <td>
            <xsl:if test="string-length(translate(., $IsNumeric, '')) = 0">
              <xsl:attribute name="align">right</xsl:attribute>
            </xsl:if>
            <xsl:value-of select="."/>
          </td>
        </xsl:for-each>
      </tr>
      </xsl:for-each>
    </table>
    <div style="margin-top: 10px">(end of results)</div>
    </xsl:template>
</xsl:stylesheet>

